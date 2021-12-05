using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Management;
using Newtonsoft.Json;
using NvAPIWrapper;
using NvAPIWrapper.GPU;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace firstapp
{
    class Program {

    static ITelegramBotClient botClient;

    [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

    const int VK_RETURN = 0x0D;
    const int WM_KEYDOWN = 0x100;
    
    static void Main() {

      while(true)
      {
          while(!IsConnectedToInternet())
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Internet May Not be Available or there is an issue with the Internet. Trying the Reconnect.....");
            Console.ResetColor();
            Thread.Sleep(1000*10);
          }

          try{
          List<int> telegramAddresses = new List<int>(){
            621742107, 805484468
          };

          botClient = new TelegramBotClient("879673655:AAG-4VHByqoG04ykQgH8t-9VOKlwTjWJfpw");

          var me = botClient.GetMeAsync().Result;
          Console.WriteLine(
            $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
          );

          botClient.OnMessage += Bot_OnMessage;
          botClient.StartReceiving();

          var connection = new HubConnectionBuilder()
              .WithUrl("https://devanshuagarwal.azurewebsites.net/telegramBotHub")
              .WithAutomaticReconnect()
              .Build();

          connection.Closed += async (error) =>
          {
              await Task.Delay(new Random().Next(0,5) * 1000);
              await connection.StartAsync();
          };

          connection.Reconnecting += async (error) =>
          {
              await Task.Delay(new Random().Next(0,5) * 1000);
              Console.WriteLine("Connection is closed tring to reconnect");
          };

          connection.Reconnected += async (error) =>
          {
              Console.WriteLine("Connected");

              if(botClient.IsReceiving)
              {
                foreach(var id in telegramAddresses)
                {
                  SendMessageOnReconnected(id);
                }
              }

              await connection.InvokeAsync("JoinUser", telegramAddresses);
          };

           connection.StartAsync();

           connection.InvokeAsync("JoinUser", telegramAddresses);

          Console.WriteLine("Press any key to exit");

          Console.ReadKey();
          botClient.StopReceiving();
           connection.StopAsync();
          }
          catch(Exception ex)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error while program running :- {ex.Message} {ex.InnerException}");
            Console.WriteLine($"Restart Connection Again in 10 Seconds.....");
            Console.ResetColor();
            Thread.Sleep(1000*10);
            var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
          }
        }
    }

    static async void Bot_OnMessage(object sender, MessageEventArgs e) {
      if (e.Message.Text != null)
      {
        List<int> telegramAddresses = new List<int>(){
            621742107, 805484468
          };

          try{
              string message = string.Empty;

              PhysicalGPU[] gpus = PhysicalGPU.GetPhysicalGPUs();
              foreach (PhysicalGPU gpu in gpus)
              {
                  Console.WriteLine(gpu.FullName);
                  foreach (GPUThermalSensor sensor in gpu.ThermalInformation.ThermalSensors)
                  {
                      message += sensor.CurrentTemperature;
                  }
              }

              float hashRate = await GetHashRate();

              Console.WriteLine($"Received a {e.Message.Text} message in chat {e.Message.Chat.FirstName}.");

              await botClient.SendTextMessageAsync(
                chatId: e.Message.Chat,
                text:   "Temp: " + message + " °C" + "\n" + "Hashrate: " + hashRate
              );
          }
          catch(Exception exception)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error while getting the hashrate :- {exception.Message} {exception.InnerException}");
            Console.WriteLine($"Please start mining in your machine.");
            foreach(var id in telegramAddresses)
            {
              SendMessageOnHashRateIssue(id);
            }
            Console.ResetColor();
          }
      }
    }

     static async void SendMessage(int id) {
       string message = string.Empty;

       PhysicalGPU[] gpus = PhysicalGPU.GetPhysicalGPUs();
          foreach (PhysicalGPU gpu in gpus)
          {
              Console.WriteLine(gpu.FullName);
              foreach (GPUThermalSensor sensor in gpu.ThermalInformation.ThermalSensors)
              {
                  message += sensor.CurrentTemperature;
              }
          }

          float hashRate = await GetHashRate();

        await botClient.SendTextMessageAsync(
              chatId: id,
              text: $"Bot Message {DateTime.Now}\n" + "Temp: " + message + " °C" + "\n" + "Hashrate: " + hashRate
            );
     }

     static async Task<float> GetHashRate(){

      float hashRate = 0;
       using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:4067/");
                //HTTP GET
                var responseTask = client.GetAsync("summary");
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {

                    var readTask = await result.Content.ReadAsStringAsync();
                    
                    var abc = JsonConvert.DeserializeObject<dynamic>(readTask);
                    hashRate = (float)abc.hashrate/1000000;
                }
            }

            return hashRate;

     }

     static async void SendMessageOnHashRateIssue(int id){

        await botClient.SendTextMessageAsync(
              chatId: id,
              text: $"Issue with mining. Please restart your mining program."
            );

     }

     static async void SendMessageOnReconnected(int id){

        await botClient.SendTextMessageAsync(
              chatId: id,
              text: $"PC: Connected"
            );

     }

      [DllImport("wininet.dll")]  
      private extern static bool InternetGetConnectedState( out int Description, int ReservedValue ) ;  
      //Creating a function that uses the API function...  
      public static bool IsConnectedToInternet( )  
      {  
        int Desc ;  
        return InternetGetConnectedState( out Desc, 0 ) ;  
      } 

  }
}
  