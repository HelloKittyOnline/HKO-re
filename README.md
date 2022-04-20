# HKO Reverse Engineering Project

## About
This is the main repo for the Hello Kitty Online Server Project. It includes the Extractors and the Server itself

***
## Requirements

1. dotNet core 3.1
2. MySQL or MariaDB
3. Hello Kitty Online install
4. Server files: https://drive.google.com/drive/folders/1rC2jR8SoLvjNesEmTQbImeBj-di1Qura?usp=sharing
5. Tar extraction software such as 7zip or winrar
6. Git (nice to have)

***
## Setting up the Server and Extractor

**1**. Install Git (skip if downloading a zipped copy)

**2**. Install dotnet 3.1 from here: https://dotnet.microsoft.com/en-us/download/dotnet/3.1

**3**. Install Hello Kitty Online from here: https://archive.org/details/pod-19902-setup

**4**. Install MySQL (community version is fine) or MariaDB and create the admin user

The extractor assumes that you are running a 64 bit version of Windows and have installed Hello Kitty Online in the default folder. If this is not true, you will have to change this line: https://github.com/HKOServer/HKO-re/blob/main/Extractor/Program.cs#L8 to the correct path.

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

**7**. Replace these lines: https://github.com/HKOServer/HKO-re/blob/main/Server/Program.cs#L349 with your MySQL credentials.

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
