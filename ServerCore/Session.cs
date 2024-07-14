using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerCore
{
    abstract class Session
    {
        Socket socket; 
        int disconnected = 0; // 이미 연결이 끊겼는지 

        // send를 위한 변수들
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        Queue<byte[]> sendQueue = new Queue<byte[]>();
        List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();
        object sendLock = new object();


        // Session에서 선언된 각 상황별 이벤트
        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);
        public abstract void OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);


        public void Init(Socket _socket)
        {
            // 세션은 이미 연결된 소켓을 다루는 클래스
            socket = _socket; 

            // 데이터를 받기
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            // 받을 버퍼 설정
            recvArgs.SetBuffer(new byte[1024], 0, 1024); // 크기, 시작위치, 사용사이즈
            RegisterRecv(recvArgs);

            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
        }

        #region 네트워크 통신
        void RegisterRecv(SocketAsyncEventArgs args) 
        {
            // ReceiveAsync 비동기 메소드로 버퍼 받아오기
            bool pending = socket.ReceiveAsync(args);
            if (pending == false)
                OnRecvCompleted(null, args);
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // 성공적으로 데이터를 받았으면 OnRecv 함수 실행 (args의 버퍼 정보를 넘기기)
                    OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));

                    RegisterRecv(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }                  
            }
            else
            {
                Disconnect();
            }
        }

        public void Send(byte[] sendBuff)
        {
            lock (sendLock) // 동시에 여러 곳에 데이터를 보낼수있음
            {
                // 1. 보내고자 하는 버퍼(데이터)를 큐에 추가
                sendQueue.Enqueue(sendBuff);
                // 2. 만약, 대기중인 버퍼리스트가 비어있으면 데이터전송
                if (pendingList.Count == 0)
                    RegisterSend();
            }
        }

        void RegisterSend()
        {
            // 3. 쌓여있는 버퍼 큐를 모두 버퍼리스트에 삽입
            while (sendQueue.Count > 0)
            {
                byte[] buff = sendQueue.Dequeue();
                pendingList.Add(new ArraySegment<byte>(buff,0,buff.Length));
            }
            // 4. 버퍼리스트를 args의 버퍼리스트에 대입 (이렇게 안하면 에러발생)
            sendArgs.BufferList = pendingList;

            // 5. SendAsync 비동기 메소드로 클라를 향해 args에 담긴 버퍼리스트 보내기
            bool pending = socket.SendAsync(sendArgs);
            if (pending == false)
                OnSendCompleted(null, sendArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (sendLock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        // 6. 데이터 보냈으니 args의 버퍼비우기
                        OnSend(sendArgs.BytesTransferred);

                        sendArgs.BufferList = null;
                        pendingList.Clear();

                        // 7. 혹시나 보내는 사이에 큐에 버퍼가 쌓였으면 마저 전송
                        if (sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                else
                    Disconnect();
            }
        }

        public void Disconnect()
        {
            // disconnected 값이 원래 1이였으면 이미 끊긴연결이므로 리턴
            if (Interlocked.Exchange(ref disconnected, 1) == 1) return;

            OnDisconnected(socket.RemoteEndPoint);

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        #endregion
    }
}
