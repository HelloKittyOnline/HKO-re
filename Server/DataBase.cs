﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Server;

class IdManager {
    private static HashSet<int> AvalibleIds = new();
    private static int MaxId = 0;

    public static int GetId() {
        lock(AvalibleIds) {
            if(AvalibleIds.Count == 0) {
                return ++MaxId;
            }

            int id = AvalibleIds.First();
            AvalibleIds.Remove(id);
            return id;
        }
    }
    public static void FreeId(int id) {
        lock(AvalibleIds) {
            if(id == MaxId) {
                MaxId--;
                while(AvalibleIds.Contains(MaxId)) {
                    AvalibleIds.Remove(MaxId);
                    MaxId--;
                }
            } else {
                AvalibleIds.Add(id);
            }
        }
    }
}

class OrderItem {
    public int Id { get; set; }
    public int ItemId { get; set; }
    public ulong AccountId { get; set; }
}

public class DictionaryInt32Converter : JsonConverter<Dictionary<int, int>> {
    public override Dictionary<int, int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if(reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected Object");

        var value = new Dictionary<int, int>();

        while(reader.Read()) {
            if(reader.TokenType == JsonTokenType.EndObject) {
                return value;
            }

            var keyString = reader.GetString();

            if(!int.TryParse(keyString, out var keyAsInt32)) {
                throw new JsonException($"Unable to convert \"{keyString}\" to System.Int32.");
            }

            reader.Read();
            value.Add(keyAsInt32, reader.GetInt32());
        }

        throw new JsonException("Error Occurred");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<int, int> value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        foreach(var (key, val) in value) {
            writer.WriteNumber(key.ToString(), val);
        }

        writer.WriteEndObject();
    }
}

enum LoginResponse {
    Ok,
    NoUser,
    InvalidPassword,
    AlreadyOnline
}

static class Database {
    private static string _connectionString;

    private static JsonSerializerOptions jsonOptions = new() {
        Converters = { new DictionaryInt32Converter() }
    };

    public static void SetConnectionString(string str) {
        _connectionString = str;
    }

    public static LoginResponse Login(string username, string password, out PlayerData playerData, out ulong discordId) {
        playerData = null;
        discordId = 0;


        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("select * from account where username = @name");

        using var command = connection.CreateCommand();
        command.CommandText = "select * from account where username = @name";
        command.Parameters.AddWithValue("name", username);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);

        if(!reader.Read()) {
            return LoginResponse.NoUser;
        }

        var dId = reader.GetUInt64("id");
        discordId = dId;
        if(Program.clients.Any(x => x.DiscordId == dId)) {
            return LoginResponse.AlreadyOnline;
        }

        var buff = new byte[48];

        reader.GetBytes("password", 0, buff, 0, 48);

        if(!VerifyPassword(password, buff)) {
            return LoginResponse.InvalidPassword;
        }

        if(!reader.IsDBNull("data")) {
            var data = reader.GetString("data");
            playerData = JsonSerializer.Deserialize<PlayerData>(data, jsonOptions);
        }

        return LoginResponse.Ok;
    }

    public static void LogOut(ulong discordId, PlayerData data) {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("update account set data = @data where id = @discordId");

        using var command = connection.CreateCommand();
        command.CommandText = "update account set data = @data where id = @discordId";
        command.Parameters.AddWithValue("discordId", discordId);

        if(data == null) {
            command.Parameters.AddWithValue("data", null);
        } else {
            command.Parameters.AddWithValue("data", JsonSerializer.Serialize(data, jsonOptions));
        }

        command.ExecuteNonQuery();
    }

    public static OrderItem[] GetOrders(ulong userId) {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("select * from orders where accountId = @userId");

        using var command = connection.CreateCommand();
        command.CommandText = "select * from orders where accountId = @userId";
        command.Parameters.AddWithValue("userId", userId);

        using var reader = command.ExecuteReader();

        var items = new List<OrderItem>();
        while(reader.Read()) {
            items.Add(new OrderItem {
                Id = reader.GetInt32("id"),
                ItemId = reader.GetInt32("itemId"),
                AccountId = reader.GetUInt64("accountId")
            });
        }

        return items.ToArray();
    }

    public static OrderItem GetOrder(ulong userId, int orderId) {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("select * from orders where accountId = @userId");

        using var command = connection.CreateCommand();
        command.CommandText = "select * from orders where id = @id";
        command.Parameters.AddWithValue("id", orderId);

        using var reader = command.ExecuteReader();

        if(!reader.Read())
            return null;

        var accountId = reader.GetUInt64("accountId");
        if(accountId != userId)
            return null;

        return new OrderItem {
            Id = reader.GetInt32("id"),
            ItemId = reader.GetInt32("itemId"),
            AccountId = accountId
        };
    }

    public static void DeleteOrder(int orderId) {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("DELETE FROM orders WHERE id = @id");

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM orders WHERE id = @id";
        command.Parameters.AddWithValue("id", orderId);

        command.ExecuteNonQuery();
    }

    public static int GetRegisteredUserCount() {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        LogRequest("SELECT COUNT(*) FROM account");

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM account";

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void LogRequest(string query) {
        var logger = Program.loggerFactory.CreateLogger("Database");
        logger.LogTrace($"Executing Query \"{query}\"");
    }

    private static byte[] HashPassword(byte[] salt, string password) {
        var rfc = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA1); // consider switching hashing algorithm
        return rfc.GetBytes(256 / 8);
    }

    private static bool VerifyPassword(string password, byte[] account) {
        var hash = HashPassword(account[..16], password);
        return hash.SequenceEqual(account[16..]);
    }
}
