﻿using EpinelPS.Utils;
using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Msgs.User
{
    [PacketPath("/User/Get")]
    public class GetUser : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            var req = await ReadData<ReqGetUserData>();
            var response = new ResGetUserData();
            var user = GetUser();

            var battleTime = DateTime.UtcNow - user.BattleTime;
            var battleTimeMs = (long)(battleTime.TotalNanoseconds / 100);


            response.User = LobbyHandler.CreateNetUserDataFromUser(user);
            response.ResetHour = 20;
            response.OutpostBattleTime = new NetOutpostBattleTime() { MaxBattleTime = 864000000000, MaxOverBattleTime = 12096000000000, BattleTime = battleTimeMs };
            response.OutpostBattleLevel = user.OutpostBattleLevel;
            response.IsSimple = req.IsSimple;

            foreach (var item in user.Currency)
            {
                response.Currency.Add(new NetUserCurrencyData() { Type = (int)item.Key, Value = item.Value });
            }
            response.RepresentationTeam = user.RepresentationTeamData;

            response.LastClearedNormalMainStageId = user.LastNormalStageCleared;

            // Restore completed tutorials. GroupID is the first 4 digits of the Table ID.
            foreach (var item in user.ClearedTutorialData)
            {
                int groupId = item.Value.GroupId;
                int version = item.Value.VersionGroup;

                response.User.Tutorials.Add(new NetTutorialData() { GroupId = groupId, LastClearedTid = item.Key, LastClearedVersion = version });
            }

            response.CommanderRoomJukeboxBgm = new NetJukeboxBgm() { JukeboxTableId = user.CommanderMusic.TableId, Type = NetJukeboxBgmType.NetJukeboxBgmTypeJukeboxTableId, Location = NetJukeboxLocation.NetJukeboxLocationCommanderRoom };
            response.LobbyJukeboxBgm = new NetJukeboxBgm() { JukeboxTableId = user.LobbyMusic.TableId, Type = NetJukeboxBgmType.NetJukeboxBgmTypeJukeboxTableId, Location = NetJukeboxLocation.NetJukeboxLocationLobby };

            await WriteDataAsync(response);
        }
    }
}
