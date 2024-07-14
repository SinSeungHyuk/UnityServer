using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class GameSession : Session // 각 상황별 발생할 이벤트 구현부
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected");

            byte[] sendBuff = Encoding.UTF8.GetBytes("Server programming");
            Send(sendBuff); // Session 클래스의 Send -> RegisterSend -> OnSendCompleted

            // 클라이언트 소켓 닫기
            Thread.Sleep(100);
            Disconnect();
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected");
        }

        public override void OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"from client : {recvData}");
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Server Send : {numOfBytes} bytes");
        }
    }

    class Program
    {
        static Listener listener = new Listener();

        static void Main(string[] args)
        {
            Console.WriteLine("==== Server ====\n");

            // DNS (Domain Name System) : 도메인 이름으로 서버주소 사용
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 7777); // 7777 : 포트번호

            // GameSession을 반환타입으로 넘겨주는 람다식 넘겨주기 (세션 종류는 많을수있음)
            listener.Init(endPoint, () => { return new GameSession(); });
            Console.WriteLine("Listening now..");

            while (true)
            {
            }
        }
    }
}
