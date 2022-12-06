# HKO Reverse Engineering Project

Join us on Discord! https://discord.gg/7Yh52hU2NE

***
## About
This is the main repo for the Hello Kitty Online Server Project. It includes the Extractors and the Server itself
***
## Joining the Public Server

If you want to join the public server, look no further! The instructions below will help you get set up. If you want to set up a local server for testing/development, jump to [Requirements](#requirements-for-self-hosted-server)

### Things You'll need:
1) Hello Kitty Online Installer: https://archive.org/details/pod-19902-setup
2) Clean Flash Player Installer: https://gitlab.com/cleanflash/installer/-/releases
3) An internet browser
4) A Discord account
5) That's it!

### Steps:
1) Install Hello Kitty Online like normal, accepting all of the standard install options.
2) Install Clean Flash Player. The minigames require installing a Clean Flash Player build from the link above. Accept all the standard options.

There are two ways to get the update files. One is through our autoupdate application (recommended) and the other way is through the client itself (legacy). Our autoupdate application removes the speed cap when downloading making it a better choice for fast connections. If installing via the client, skip to step 5. Otherwise, continue.

3) Download the latest autoupdate application from here: https://github.com/HelloKittyOnline/HKO-re/releases/tag/Autoupdate After running it, it should start downloading the update files
4) Skip to step 10

5) After Hello Kitty is Installed, open this file in notepad or any other text editor: `C:\Program Files (x86)\SanrioTown\Hello Kitty Online\Leading.ini`
6) Change the text to say `http://hko.evidentfla.me:8080/single/leading.txt` (You may need administrator privileges to save the changes)
7) Navigate to https://hko.evidentfla.me in your web browser, sign in with your Discord account, and create your username and password (You will use this to sign in to HKO)
8) Run Hello Kitty Online from your Desktop or Start Menu
9) It will now download and update Hello Kitty Online with the latest maps and files. Ignore the 403 error as that is normal. This will take a long time as HKO itself throttles the download speed, not the server. Go to the store, come back, eat dinner, and *maybe* it will be done. Your internet speed doesn't necessarily matter.
10) Once all of the files have finished downloading, Click `Start Game` and you'll be at the login screen! `Patch Now` does not do anything.
11) Login with the username and password you created earlier
12) Select `World 1` and enter the game!
13) Have Fun!

If you find any issues:

1) Check the [Issues](https://github.com/HelloKittyOnline/HKO-re/issues) tab! We may already know of the issue and are working on it!
2) Check the [Discord Server](https://discord.gg/7Yh52hU2NE)! We are willing to help figure out any issues you may have in the #bugs channel.
3) Open an Issue!

***

## Requirements for Self-Hosted Server

1. dotNet 6.0
2. MySQL or MariaDB
3. Hello Kitty Online install
4. Server files: https://drive.google.com/drive/folders/1rC2jR8SoLvjNesEmTQbImeBj-di1Qura?usp=sharing
5. Tar extraction software such as 7zip or winrar
6. Git (nice to have)

***
## Setting up the Server and Extractor

**1**. Install Git (skip if downloading a zipped copy)

**2**. Install dotnet 6.0 from here: https://dotnet.microsoft.com/en-us/download/dotnet/6.0

**3**. Install Hello Kitty Online from here: https://archive.org/details/pod-19902-setup

**4**. Install MySQL (community version is fine) or MariaDB and create the admin user

The extractor assumes that you are running a 64 bit version of Windows and have installed Hello Kitty Online in the default folder. If this is not true, you will have to change this line: https://github.com/HelloKittyOnline/HKO-re/blob/main/Extractor/Program.cs#L8 to the correct path.

**5**. Open a MySQL or MariaDB console and run the following (including the semicolons!):
```
CREATE DATABASE hko;

USE hko;

CREATE TABLE account (
  id bigint NOT NULL PRIMARY KEY,
  username tinytext NOT NULL UNIQUE KEY,
  password tinyblob NOT NULL,
  data text DEFAULT NULL
);
```

**6**. Either download a zip of the repository and extract, or download with git by using the command: `git clone https://github.com/HKOServer/HKO-re`

This will create a folder called `HKO-re`.

**7**. Replace these lines: https://github.com/HelloKittyOnline/HKO-re/blob/main/Server/Program.cs#L349 with your MySQL credentials.

**8**. Open an administrator PowerShell instance (search for Powershell, right click, run as admin)

**9**. `cd` to the HKO-re folder

**10**. run `dotnet build`

This will build both the extractor and the server

***
## Running the Extractor

**1**. After building, the output file will be in the `bin` folder.

**2**. Run `dotnet Extractor.dll` as an administrator

This will extract and patch your Hello Kitty Online instance to connect to a local server.

***
## Running the Server

**1**. After building, the output file will be in the `bin` folder.

**2**. Copy all sdb files from the "Server files" link above and place them in the same folder as `Server.dll`

**3**. Run `dotnet Server.dll` in a PowerShell prompt

**4**. Change the `Leading.ini` file in the Hello Kitty Online Program Files directory to `http://127.0.0.1/single/leading.txt`

***
## Creating the user

**1**. Open a MySQL prompt

**2**. Run the following:
```
INSERT INTO `account` (`id`, `username`, `password`, `data`) VALUES (1, 'test', 0x73b5a6bead178fe1b7442d5944ce297398677173e9a6b3eaa591982510fff09c91797af05be14b8364ee39e5959cb161, null);
```

This will create a user called `test` with the password `asdasdasd`. Please do not use this in production :)

***
## Updating Hello Kitty Online

Now, we need to update HKO so all of the maps are installed.

**1**. Download all the tar files from the "Server files" link.

**2**. Extract `data_0.tar`,`data_1.tar`,`data_2.tar`, and `data_3.tar` into the Hello Kitty Online Program Files data folder.

**3**. Extract `tables.tar` into the Hello Kitty Online Program Files tables folder.

**4**. Replace the text in every file in the `ver` folder with `v0109090007` **EXCEPT FOR** `version_pc.txt`!

***
## Booting Hello Kitty Online

After this, you should be all set! To start Hello Kitty Online:

**1**. Run the `Autoupdate.exe` application directly or double click on the desktop HKO icon

**2**. A screen that looks like it is hanging will appear. Wait for the green check mark to appear.

**3**. Click the green check mark and login as the `test` user you created above!
