﻿using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Paseto.Builder;
using Paseto;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Msgs.Auth
{
    [PacketPath("/auth/enterserver")]
    public class GetUserOnlineStateLog : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            var req = await ReadData<ReqEnterServer>();

            // request has auth token
            UsedAuthToken = req.AuthToken;
            foreach (var item in JsonDb.Instance.LauncherAccessTokens)
            {
                if (item.Token == UsedAuthToken)
                {
                    UserId = item.UserID;
                }
            }
            if (UserId == 0) throw new BadHttpRequestException("unknown auth token", 403);
            var user = GetUser();

            var rsp = LobbyHandler.GenGameClientTok(req.ClientPublicKey, UserId);

            var token = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local)
                               .WithKey(JsonDb.Instance.LauncherTokenKey, Encryption.SymmetricKey)
                               .AddClaim("userid", UserId)
                               .IssuedAt(DateTime.UtcNow)
                               .Expiration(DateTime.UtcNow.AddDays(2))
                               .Encode();

            var encryptionToken = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local)
                               .WithKey(JsonDb.Instance.LauncherTokenKey, Encryption.SymmetricKey)
                               .AddClaim("data", JsonConvert.SerializeObject(rsp))
                               .IssuedAt(DateTime.UtcNow)
                               .Expiration(DateTime.UtcNow.AddDays(2))
                               .Encode();


            var response = new ResEnterServer();
         
            response.GameClientToken = token;
            response.FeatureDataInfo = new NetFeatureDataInfo() { UseFeatureData = true };
            response.Identifier = new NetLegacyUserIdentifier() { Server = 1000, Usn = (long)user.ID };
            response.ShouldRestartAfter = Duration.FromTimeSpan(TimeSpan.FromSeconds(86400));
            
            response.EncryptionToken = ByteString.CopyFromUtf8(encryptionToken);
            await WriteDataAsync(response);
        }
    }
}
