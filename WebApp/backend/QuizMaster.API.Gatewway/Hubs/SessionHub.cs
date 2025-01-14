﻿using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QuizMaster.API.Account.Proto;
using QuizMaster.API.Authentication.Proto;
using QuizMaster.API.Gateway.Configuration;
using QuizMaster.API.Gateway.Attributes;
using QuizMaster.API.Gateway.Services;
using QuizMaster.API.Gateway.Utilities;
using QuizMaster.API.QuizSession.Models;
using QuizMaster.API.QuizSession.Protos;
using QuizMaster.Library.Common.Entities.Rooms;
using QuizMaster.Library.Common.Models.QuizSession;
using System.Linq;
using System.Threading.Channels;
using QuizMaster.API.Authentication.Models;
using Grpc.Core;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;

namespace QuizMaster.API.Gateway.Hubs
{

    public class SessionHub : Hub
    {
        private GrpcChannel _channel;
        private QuizRoomService.QuizRoomServiceClient _channelClient;
        private readonly AuthService.AuthServiceClient _authChannelClient;
        private SessionHandler SessionHandler;
        private QuizHandler QuizHandler;

        public SessionHub(IOptions<GrpcServerConfiguration> options, SessionHandler sessionHandler, QuizHandler quizHandler)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            _channel = GrpcChannel.ForAddress(options.Value.Session_Service, new GrpcChannelOptions { HttpHandler = handler });
            _channelClient = new QuizRoomService.QuizRoomServiceClient(_channel);
            _channel = GrpcChannel.ForAddress(options.Value.Authentication_Service, new GrpcChannelOptions { HttpHandler = handler });
            _authChannelClient = new AuthService.AuthServiceClient(_channel);
            SessionHandler = sessionHandler;
            QuizHandler = quizHandler;
        }

        /*
         * Login the User
         */
        public async Task Login(string token)
        {
            var connectionId = Context.ConnectionId;
            await SessionHandler.AuthenticateConnectionId(this, _authChannelClient, connectionId, token);

        }

        public async Task CreateRoom(CreateRoomDTO roomDTO)
        {
         
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Failed to join room, you are not authorized");
                return;
            }
            // Grab the UserInformation
            var userInfo = await SessionHandler.GetUserInformation(_authChannelClient, SessionHandler.GetConnectionToken(connectionId));
            if (userInfo == null || userInfo.UserData == null)
            {
                // Sending an error message to the client
                await Clients.Caller.SendAsync("notif", "Invalid user information in the token");
                return;
            }

            var userName = userInfo.UserData.UserName;
            var userId = userInfo.UserData.Id;
            var userRole = userInfo.Roles.Any(h => h.Equals("Administrator")) ? "Administrator" : "User";

            var headers = new Metadata
            {
                { "username", userName ?? "unknown" },
                { "id", userId.ToString() ?? "unknown" },
                { "role", userRole }
            };

            if (!SessionHandler.IsAdmin(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Please contact administrator");
                return;
            }

            var request = new CreateRoomRequest { Room = JsonConvert.SerializeObject(roomDTO) };
            var reply = await _channelClient.CreateRoomAsync(request, headers);

            if (reply.Code == 200)
            {


                var quizRoom = JsonConvert.DeserializeObject<QuizRoom>(reply.Data);
                await Groups.AddToGroupAsync(Context.ConnectionId, quizRoom.QRoomPin + "");

                await Clients.All.SendAsync("NewQuizRooms", new[] { quizRoom });
            }


            // TODO: Reply 500 status
        }


        public async Task DeleteRoom(int roomId)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Failed to join room, you are not authorized");
                return;
            }
            // Grab the UserInformation
            var userInfo = await SessionHandler.GetUserInformation(_authChannelClient, SessionHandler.GetConnectionToken(connectionId));
            if (userInfo == null || userInfo.UserData == null)
            {
                // Sending an error message to the client
                await Clients.Caller.SendAsync("notif", "Invalid user information in the token");
                return;
            }

            var userName = userInfo.UserData.UserName;
            var userId = userInfo.UserData.Id;
            var userRole = userInfo.Roles.Any(h => h.Equals("Administrator")) ? "Administrator" : "User";

            var headers = new Metadata
            {
                { "username", userName ?? "unknown" },
                { "id", userId.ToString() ?? "unknown" },
                { "role", userRole }
            };
            if (!SessionHandler.IsAdmin(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Please contact administrator");
                return;
            }

            var request = new ModifyRoomRequest { Room = roomId };

            var reply = await _channelClient.DeleteRoomAsync(request, headers);

            if (reply.Code == 204)
            {
                await Clients.Caller.SendAsync("notif", "Room was deleted");
                await Clients.Group(reply.Data).SendAsync("notif", "[System] You have been removed from the room");
                await SessionHandler.RemoveGroup(this, reply.Data);
            }
            else
            {
                await Clients.Caller.SendAsync("notif", reply.Message);
            }
        }

        public async Task UpdateRoom(UpdateRoomDTO updateRoomDTO)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Failed to join room, you are not authorized");
                return;
            }
            // Grab the UserInformation
            var userInfo = await SessionHandler.GetUserInformation(_authChannelClient, SessionHandler.GetConnectionToken(connectionId));
            if (userInfo == null || userInfo.UserData == null)
            {
                // Sending an error message to the client
                await Clients.Caller.SendAsync("notif", "Invalid user information in the token");
                return;
            }

            var userName = userInfo.UserData.UserName;
            var userId = userInfo.UserData.Id;
            var userRole = userInfo.Roles.Any(h => h.Equals("Administrator")) ? "Administrator" : "User";

            var headers = new Metadata
            {
                { "username", userName ?? "unknown" },
                { "id", userId.ToString() ?? "unknown" },
                { "role", userRole }
            };

            if (!SessionHandler.IsAdmin(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Please contact administrator");
                return;
            }
            var request = new CreateRoomRequest { Room = JsonConvert.SerializeObject(updateRoomDTO) };
            var reply = await _channelClient.UpdateRoomAsync(request, headers);

            // TODO:
            if (reply.Code == 200)
            {
                var quizRoom = JsonConvert.DeserializeObject<QuizRoom>(reply.Data);
                await Clients.Caller.SendAsync("NewQuizRooms", new[] { quizRoom });
                await Clients.Caller.SendAsync("notif", "Room was updated");
            }
            else
            {
                await Clients.Caller.SendAsync("notif", reply.Message);
            }
        }

        public async Task GetConnectionId()
        {
            string connectionId = Context.ConnectionId;
            await Clients.Caller.SendAsync("connId", connectionId);
        }

        /*
         * Unused SignalR connection, use the QuizSetGatewayController's SubmitAnswer route
         */
        public async Task SubmitAnswer(string questionId, string answer)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "You need to login to submit an answer");
                return;
            }
            try
            {
                await Clients.Caller.SendAsync("notif", $"Submitting: {answer}");
                await SessionHandler.SubmitAnswer(_channelClient, connectionId, Convert.ToInt32(questionId), answer);
            }
            catch { await Clients.Caller.SendAsync("notif", $"Failed to submit answer: {answer}"); }
        }

        public async Task KickParticipant(int participantUserId, int roomPin)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAdmin(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Admin can only join use this function");
                return;
            }
            var participant = SessionHandler.GetConnectionIdsInAGroup(roomPin.ToString());
            if (participant == null)
            {
                await Clients.Caller.SendAsync("notif", "Could not kick participant");
                return;
            }
            foreach (var connId in SessionHandler.GetConnectionIdsInAGroup(roomPin.ToString()))
            {
                var quizParticipant = SessionHandler.GetLinkedParticipantInConnectionId(connId);
                if (quizParticipant == null) continue;
                if (quizParticipant.UserId != participantUserId) continue;
                if (connectionId == connId)
                {
                    await Clients.Client(connId).SendAsync("chat", new { Message = "You cannot kick yourself", Name = "bot", IsAdmin = false });
                    return;
                }

                await Clients.Client(connId).SendAsync("chat", new { Message = "You are kicked from the room", Name = "bot", IsAdmin = false });
                await SessionHandler.RemoveClientFromGroups(this, connId, $"{quizParticipant.QParticipantDesc} was kicked by admin");
                SessionHandler.UnbindConnectionId(connId);
                IEnumerable<object> participants = SessionHandler.GetParticipantLinkedConnectionsInAGroup(roomPin.ToString()).Select(p => new { p.UserId, p.QParticipantDesc });
                await Clients.Group($"{roomPin}").SendAsync("participants", participants);
                await Clients.Client(connId).SendAsync("kicked", "/* Triggered, you are kicked boyo */");
            }

        }


        public async Task Chat(string chat, int roomId)
        {
            string connectionId = Context.ConnectionId;

            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("chat", "You need to login to join chat");
                return;
            }
            var participantData = SessionHandler.GetLinkedParticipantInConnectionId(connectionId);
            if (participantData == null) { return; }
            // send chat only to group
            if (SessionHandler.IsAdmin(connectionId))
                await Clients.Group(roomId.ToString()).SendAsync("chat", new { Message = chat, Name = participantData.QParticipantDesc, IsAdmin = true });
            else await Clients.Group(roomId.ToString()).SendAsync("chat", new { Message = chat, Name = participantData.QParticipantDesc, IsAdmin = false });
        }

        public async Task JoinRoom(int RoomPin)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAuthenticated(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Failed to join room, you are not authorized");
                return;
            }
            // Grab the UserInformation
            var userData = await SessionHandler.GetUserInformation(_authChannelClient, SessionHandler.GetConnectionToken(connectionId));
            if (userData == null)
            {
                await Clients.Caller.SendAsync("notif", $"There was an issue authenticating your account");
                return;
            }

            try
            {
                var reply = _channelClient.GetAllRoom(new RoomsEmptyRequest());

                if (reply.Code == 200)
                {
                    var quizRooms = JsonConvert.DeserializeObject<QuizRoom[]>(reply.Data);

                    bool containsId = false;
                    var room = new QuizRoom();
                    foreach (QuizRoom rooms in quizRooms)
                    {
                        if (rooms.QRoomPin == RoomPin)
                        {
                            room = rooms;
                            containsId = true;
                            break;
                        }
                    }

                    if (containsId)
                    {
                        //string Name = NAMES[new Random().Next(0, NAMES.Count - 1)];
                        string Name = userData.UserData.UserName;
                        SessionHandler.LinkParticipantConnectionId(connectionId, new QuizParticipant { QParticipantDesc = Name, UserId = userData.UserData.Id, QRoomId = room.Id });

                        // get the linked participant and check if eliminated
                        var ParticipantData = SessionHandler.GetLinkedParticipantInConnectionId(connectionId);
                        if (ParticipantData == null) return;
                        if (SessionHandler.IsParticipantEliminated(RoomPin, ParticipantData))
                        {
                            await Clients.Caller.SendAsync("notif", "You are eliminated on this quiz, you cannot join");
                            return;
                        }

                        // before joining, check if room allows reconnection or joining on session is active
                        if (SessionHandler.IsRoomActive(RoomPin))
                        {
                            var activeRoom = SessionHandler.GetActiveRoom(RoomPin);
                            if (activeRoom != null)
                            {
                                var joineeData = SessionHandler.GetLinkedParticipantInConnectionId(connectionId);
                                var participantData = SessionHandler.GetParticipantLinkedConnectionsInAGroup(RoomPin.ToString());

                                // check if participant already exist in the active room
                                if (participantData != null && joineeData != null)
                                {
                                    if (participantData.Where(p => p.UserId == joineeData.UserId).Any())
                                    {
                                        if (!activeRoom.AllowReconnect())
                                        {
                                            await Clients.Caller.SendAsync("notif", $"Sorry but disconnected participant is not allowed to join again.");
                                            return;
                                        }
                                    }
                                }

                                if (!activeRoom.AllowJoinOnQuizStarted())
                                {
                                    await Clients.Caller.SendAsync("notif", $"Sorry but the quiz has already been started.");
                                    return;
                                }
                            }
                        }
                        await Clients.Caller.SendAsync("JoinFailed", false);
                        await Task.Delay(1000);
                        
                        await SessionHandler.AddToGroup(this, $"{RoomPin}", connectionId);
                        await Clients.Group($"{RoomPin}").SendAsync("chat", new { Message = $"{Name} has joined the room", Name = "bot", IsAdmin = false });
                        IEnumerable<object> participants = SessionHandler.GetParticipantLinkedConnectionsInAGroup(RoomPin.ToString()).Select(p => new { p.UserId, p.QParticipantDesc });
                        await Clients.Group($"{RoomPin}").SendAsync("participants", participants);
                    }
                    else {
                        await Clients.Caller.SendAsync("JoinFailed", true);
                    }
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("notif", $"An error has occurred while trying to connect");
                Console.Write(ex.ToString());
            }
        }

        public async Task GetAllRooms()
        {
            try
            {
                var reply = _channelClient.GetAllRoom(new RoomsEmptyRequest());

                if (reply.Code == 200)
                {
                    var quizRooms = JsonConvert.DeserializeObject<QuizRoom[]>(reply.Data);

                    await Clients.All.SendAsync("QuizRooms", quizRooms);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }

        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await LeaveRoom();
        }

        public async Task LeaveRoom()
        {
            var participantData = SessionHandler.GetLinkedParticipantInConnectionId(Context.ConnectionId);
            if (participantData == null) { return; }
            var group = SessionHandler.GetConnectionGroup(Context.ConnectionId);
            await SessionHandler.RemoveClientFromGroups(this, Context.ConnectionId, $"{participantData.QParticipantDesc} has left the room", sendParticipantData: false);
            SessionHandler.UnbindConnectionId(Context.ConnectionId);

            if (group != null)
            {
                IEnumerable<object> participants = SessionHandler.GetParticipantLinkedConnectionsInAGroup(group).Select(p => new { p.UserId, p.QParticipantDesc });
                await Clients.Group(group).SendAsync("participants", participants);
            }
        }

        public async Task StartRoom(string roomPin)
        {
            string connectionId = Context.ConnectionId;
            if (!SessionHandler.IsAdmin(connectionId))
            {
                await Clients.Caller.SendAsync("notif", "Please contact administrator");
                return;
            }
            try
            {
                var reply = _channelClient.GetAllRoom(new RoomsEmptyRequest());
                QuizRoom? quizRoom = null;

                if (reply.Code == 200)
                {
                    var quizRooms = JsonConvert.DeserializeObject<QuizRoom[]>(reply.Data);

                    var room = new QuizRoom();
                    foreach (QuizRoom rooms in quizRooms)
                    {
                        if (rooms.QRoomPin == Convert.ToInt32(roomPin))
                        {
                            quizRoom = rooms;
                            break;
                        }
                    }
                }

                if (quizRoom == null)
                {
                    await Clients.Caller.SendAsync("notif", "Failed to start a session");
                    return;
                }
                // we will not use await, we will let the request pass
                await Clients.Group(roomPin).SendAsync("start", true);
                //await SessionHandler.StartQuiz(this, _channelClient, roomId.ToString());
                await Task.Delay(500);
                var sessionId = SessionHandler.GenerateSessionId(roomPin); // once started, generate a session Id, will be used for report tracking
                await QuizHandler.StartQuiz(this, SessionHandler, _channelClient, quizRoom, sessionId);
            }
            catch (Exception ex)
            {
                Console.Write(ex?.ToString());
            }
        }

        public async Task GetRoomParticipants(string roomPin)
        {
            await Task.Delay(1000);
            var participants = SessionHandler.GetParticipantLinkedConnectionsInAGroup(roomPin);
            await Clients.Group(roomPin).SendAsync("participants", participants);
        }

      

    }
}