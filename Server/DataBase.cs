using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Server {
    class Account {
        public string Username { get; set; }

        public byte[] Salt { get; set; }
        public byte[] Password { get; set; }

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