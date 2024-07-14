using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class Listener
    {
        Socket listenSocket;
        // Func<T> : 매개변수 없고 T타입을 반환하는 함수
        Func<Session> sessionFactory; // 연결된 세션이 어떤 세션인지 알기위한 Func

        public void Init(IPEndPoint endPoint, Func<Session> _sessionFactory)
        {
            listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sessionFactory += _sessionFactory;

            // Socket 바인딩
            listenSocket.Bind(endPoint);
            // Socket 활성화
            listenSocket.Listen(10); // 최대 서버대기수 10명

            // Accept에 필요한 이벤트Args 생성
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            // 해당 이벤트가 성공했으면 OnAcceptCompleted 콜백함수 실행
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            // 최초로 소켓 연결시도
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            // 최초 연결 이후에 다음 연결을 대비해서 초기화
            args.AcceptSocket = null;

            // pending : 보류중 
            // AcceptAsync : 비동기적 함수로 연결시도
            bool pending = listenSocket.AcceptAsync(args);
            if (pending == false) // 연결 성공 (보류중이 아니므로)
                OnAcceptCompleted(null, args);
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            // 소켓 연결이 에러없이 성공
            if (args.SocketError == SocketError.Success)
            {
                // Listener 클래스에선 클라 연결을 요청받았으니 할 일을 다함
                // Program에서 등록한 Session으로 연결된 소켓 넘겨주기 
                Session session = sessionFactory?.Invoke();
                session.Init(args.AcceptSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            // 이미 연결된 소켓은 처리하고 다시 연결시도 시작하기
            RegisterAccept(args);
        }
    }
}
