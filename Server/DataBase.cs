using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Server {
    class Account {
        public string Username { get; set; }
        public string Password { get; set; }
        public PlayerData PlayerData { get; set; }
    }

    class DataBase {
        public Dictionary<string, Account> Accounts { get; set; }
        public int IdCounter { get; set; }

        public DataBase() {
            Accounts = new Dictionary<string, Account>();
        }

        public void Save(string path) {
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }
        public static DataBase Load(string path) {
            return File.Exists(path) ? JsonSerializer.Deserialize<DataBase>(File.ReadAllText(path)) : new DataBase();
        }

        public Account GetPlayer(string username, string password) {
            if(Accounts.TryGetValue(username, out var account)) {
                return account.Password == password ? account : null;
            }

            Console.WriteLine($"Created account {username}");
            // create new account
            account = new Account {
                Username = username,
                Password = password
            };
            Accounts[username] = account;
            return account;
        }
    }
}