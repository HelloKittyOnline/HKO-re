# HKO Reverse Engineering Project

Join us on Discord! https://discord.gg/7Yh52hU2NE

***
## About
This is the main repo for the Hello Kitty Online Server Project. It includes the Server, the Launcher, and a Resource Extractor.
***

## Joining the Public Server
If you want to join the public server, look no further! The instructions below will help you get set up. If you want to set up a local server for testing/development, jump to [Setting up dev enviroment](#setting-up-dev-enviroment)

### Install using the launcher (Recommended)
Download and run the latest [launcher](https://github.com/HelloKittyOnline/HKO-re/releases/latest) application. Continue with [Logging in](#logging-in)

### Manual install
1) Install [Hello Kitty Online](https://archive.org/details/pod-19902-setup) like normal, accepting all of the standard install options. 
2) Install [Clean Flash Player](https://gitlab.com/cleanflash/installer/-/releases). The minigames require having a Flash Player installed. Accept **all** the standard options. 
3) After Hello Kitty is Installed, start notepad or any other text editor as administrator and open `C:\Program Files (x86)\SanrioTown\Hello Kitty Online\Leading.ini`
4) Change the text to say `http://hko.evidentfla.me:8080/single/leading.txt` 
5) Run Hello Kitty Online from your Desktop or Start Menu. 
6) It will now download and update Hello Kitty Online with the latest maps and files. Ignore the 403 error as that is normal. This will take a long time as HKO itself throttles the download speed, not the server. 
7) Once all of the files have finished downloading, Click `Start Game` and you'll be at the login screen! `Patch Now` does not do anything.

### Logging in
1) Navigate to https://hko.evidentfla.me in your web browser, sign in with your Discord account, and set your username and password.
2) Login with the username and password you just created
3) Select `World 1` and enter the game!
4) Have Fun!

If you have any issues:

1) Check the [Issues](https://github.com/HelloKittyOnline/HKO-re/issues) tab! We may already know of the issue and are working on it!
2) Check the [Discord Server](https://discord.gg/7Yh52hU2NE)! We are willing to help figure out any issues you may have in the #bugs channel.
3) Open an Issue!

***

## Setting up a local server/dev enviroment
The Server setup is quite complicated and individual steps often change.
Join the discord server and ask in #general-dev for setup help if you run into issues.

1) Download and install [dotnet 7](https://dotnet.microsoft.com/en-us/download)
2) Download and install the [Hello Kitty Online client](https://archive.org/details/pod-19902-setup)
3) Download the [Data files](https://drive.google.com/drive/folders/1rC2jR8SoLvjNesEmTQbImeBj-di1Qura?usp=sharing) and extract everything in the core folder in your HKO install directory.

This makes sure that your client is fully up to date.

4) Download and install [MariaDB 10](https://mariadb.org/download/). Standard MySQL is no longer supported.
5) Download and install [Clean Flash Player](https://gitlab.com/cleanflash/installer/-/releases)
6) Clone this repo to any directory and open a terminal prompt in said directory
7) Open a Mariadb console and input the following:
```
CREATE DATABASE hko;
USE hko;
CREATE TABLE account (
  id bigint NOT NULL PRIMARY KEY,
  username tinytext NOT NULL UNIQUE KEY COLLATE utf8mb4_general_ci,
  password tinyblob NOT NULL,
  data text DEFAULT NULL
);
```
This creates the account database

8) Replace these lines in the Program.cs with your MySQL credentials: https://github.com/HelloKittyOnline/HKO-re/blob/main/Server/Program.cs#L324
9) Replace these lines in the Extractor Program.cs with your paths: https://github.com/HelloKittyOnline/HKO-re/blob/main/Extractor/Program.cs#L86
10) Run `dotnet build`

This will compile the server and Extractor tool.

11) Run the Extractor.exe in the Extractor bin directory to patch your Hello Kitty Online client

12) In the MariaDB console, input the following:
```
INSERT INTO `account` (`id`, `username`, `password`, `data`) VALUES (1, 'test', 0x73b5a6bead178fe1b7442d5944ce297398677173e9a6b3eaa591982510fff09c91797af05be14b8364ee39e5959cb161, null);
```
This will create a user called `test` with the password `asdasdasd`

13) Extract tables.tar from the [Data files](https://drive.google.com/drive/folders/1rC2jR8SoLvjNesEmTQbImeBj-di1Qura?usp=sharing) link into the same directory as Server.exe
14) Copy all [Data JSONs](https://github.com/HelloKittyOnline/hko_data/tree/main/data) to the same directory as the Server.exe file
15) Run `dotnet Server.dll` to launch the Server. It should say `Listening at :25000`
16) Launch HKO and login with the test credentials you created above
17) You should be in the game now!
