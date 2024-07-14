using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==== Client ====\n");

            // DNS (Domain Name System) : 도메인 이름으로 서버주소 사용
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777); // 7777 : 포트번호

            while (true)
            {
                // 클라이언트 소켓 설정
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // 클라이언트 - 서버 연결시도 (해당 ip주소,포트번호의 서버를 향해)
                    socket.Connect(endPoint);
                    Console.WriteLine($"Connected To {socket.RemoteEndPoint.ToString()}");

                    // 버퍼 서버에 보내기
                    byte[] sendBuff = Encoding.UTF8.GetBytes("Client's Buffer");
                    int sendBytes = socket.Send(sendBuff);

                    // 서버에서 온 버퍼 받기
                    byte[] recvBuff = new byte[1024];
                    int recvBytes = socket.Receive(recvBuff);
                    string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                    Console.WriteLine($"from server : {recvData}");

                    // 소켓 닫기
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(1000);
            }
        }
    }
}
