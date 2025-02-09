﻿using ChatServer.Converters;
using ChatServer.Networks;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private ConcurrentDictionary<string, RoomNetwork> _current = new();
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public ConcurrentDictionary<string, RoomNetwork> Rooms
        {
            get => _current;
            set => _current = value;
        }

        public async Task ReadFromFileAsync(string nameRoom)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                if (IsRoomExists(nameRoom))
                {
                    await using var stream = File.Open(nameRoom + ".json", FileMode.Open);
                    var serializeOptions = new JsonSerializerOptions
                    {
                        Converters =
                    {
                        new UsersBagJsonConverter()
                    }
                    };
                    if (_current.ContainsKey(nameRoom))
                    {
                        var tmp = await JsonSerializer.DeserializeAsync<RoomNetwork>(stream, serializeOptions);
                        _current[nameRoom].Users = tmp.Users;
                        _current[nameRoom].History = tmp.History;
                    }
                    else
                        AddRoom(nameRoom, await JsonSerializer.DeserializeAsync<RoomNetwork>(stream, serializeOptions));

                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        public async Task WriteAsyncToFile()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                foreach (var (key, value) in _current)
                {
                    await using FileStream streamMessage = File.Create(key + ".json");
                    await JsonSerializer.SerializeAsync<RoomNetwork>(streamMessage, value, new JsonSerializerOptions { WriteIndented = true });

                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }

        }
        public string AddRoom(string nameRoom, RoomNetwork room)
        {
            _current.TryAdd(nameRoom, room);
            return nameRoom;
        }

        public bool IsRoomExists(string nameRoom) => File.Exists(nameRoom + ".json");
        public void RemoveRoom(string nameRoom) => _current.TryRemove(nameRoom, out _);

        public RoomNetwork FindRoom(string nameRoom) => _current[nameRoom];

    }
}

