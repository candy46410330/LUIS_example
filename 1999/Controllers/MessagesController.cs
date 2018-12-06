using _1999.Serialization;
using _1999.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Configuration;
using System.Web;
using System.IO;
using _1999.Models;
using System.Diagnostics;

namespace _1999.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity message)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));

            //string resposta = "Test Message-好";
            var resposta = await Response(message); //跟LUIS取得辩試結果，並準備回覆訊息
            if (resposta != null)
            {
                /////////////計算字數/////////////////////////
                // calculate something for us to return
                int length = (message.Text ?? string.Empty).Length;

                // return our reply to the user
                //Activity msg = message.CreateReply($"You sent {message.Text} which was {length} characters", "zh-TW");
                //await connector.Conversations.ReplyToActivityAsync(msg);

                //////////////回傳LIUS結果/////////////////////
                var msg = message.CreateReply(resposta , "zh-TW");
                ///////////////測試字數////////////////////////
                //var msg = message.CreateReply(resposta + $"You sent {message.Text} which was {length} characters", "zh-TW");

                await connector.Conversations.ReplyToActivityAsync(msg); //回傳訊息
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted); //回傳狀態

        }
        private static async Task<Activity> GetLuisResponse(Activity message)
        {
            Activity resposta = new Activity();

            var luisResponse = await Luis.GetResponse(message.Text); //Call LUIS Service
            if (luisResponse != null)
            {
                var intent = new Intent();
                var entity = new Serialization.Entity();

                string weather = string.Empty; 
                string location = string.Empty;

                string entityType = string.Empty;

                int replaceStartPos = 0;
                resposta = message.CreateReply("我不識字XD");

                foreach (var item in luisResponse.entities)
                {
                    entityType = item.type;
                    replaceStartPos = entityType.IndexOf("::");
                    if (replaceStartPos > 0)
                        entityType = entityType.Substring(0, replaceStartPos);

                    switch (entityType)
                    {
                        case "天氣":
                            weather = item.entity;
                            break;
                        case "地點":
                            location = item.entity;
                            break;
                    }
                }
                    resposta = message.CreateReply($"您在問的是〔{weather}〕，地點在〔{location}〕");
            }
            return resposta;
        }
        private static async Task<string> Response(Activity message)
        {
            Activity resposta = null; //回覆給客戶訊息
            if (message != null)
            {
                switch (message.GetActivityType())
                {
                    case ActivityTypes.Message:
                        resposta = await GetLuisResponse(message);
                        break;
                    case ActivityTypes.ConversationUpdate:
                    case ActivityTypes.ContactRelationUpdate:
                    case ActivityTypes.Typing:
                    case ActivityTypes.DeleteUserData:
                    default:
                        //Trace.TraceError($"Unknown activity type ignored: {message.GetActivityType()}");
                        break;

                }
            }

            if (resposta != null)
                return resposta.Text;
            else
                return null;
        }
    }
}