using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server {
    class Account {
        public string Username { get; set; }

        public byte[] Salt { get; set; }
        public byte[] Password { get; set; }

        public PlayerData PlayerData { get; set; }
    }

    class IdManager {
        private static HashSet<int> AvalibleIds = new HashSet<int>();
        private static int MaxId = 0;

        public static int GetId() {
            if(AvalibleIds.Count == 0) {
                return ++MaxId;
            } else {
                int id = AvalibleIds.First();
                AvalibleIds.Remove(id);
                return id;
            }
        }
        public static void FreeId(int id) {
            if(id == MaxId) {
                MaxId--;
            } else {
                AvalibleIds.Add(id);
            }
        }
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

    class DataBase {
        public Dictionary<string, Account> Accounts { get; set; }

        public DataBase() {
            Accounts = new Dictionary<string, Account>();
        }

        public void Save(string path) {
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions {
                Converters = { new DictionaryInt32Converter() }
            }));
        }
        public static DataBase Load(string path) {
            var db = File.Exists(path) ? JsonSerializer.Deserialize<DataBase>(File.ReadAllText(path), new JsonSerializerOptions {
                Converters = { new DictionaryInt32Converter() }
            }) : new DataBase();
            foreach (var account in db.Accounts) {
                account.Value.PlayerData?.Init();
            }
            return db;
        }

        public Account GetPlayer(string username, string password) {
            if(Accounts.TryGetValue(username, out var account)) {
                return VerifyPassword(password, account) ? account : null;
            }

            Console.WriteLine($"Created account {username}");

            var salt = GenerateSalt();

            // create new account
            account = new Account {
                Username = username,
                Salt = salt,
                Password = HashPassword(salt, password)
            };
            Accounts[username] = account;
            return account;
        }

        private static byte[] GenerateSalt() {
            byte[] salt = new byte[128 / 8];
            var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetNonZeroBytes(salt);
            return salt;
        }

        private static byte[] HashPassword(byte[] salt, string password) {
            var rfc = new Rfc2898DeriveBytes(password, salt, 10000);
            return rfc.GetBytes(256 / 8);
        }

        private static bool VerifyPassword(string password, Account account) {
            var hash = HashPassword(account.Salt, password);
            return hash.SequenceEqual(account.Password);
        }
    }
}