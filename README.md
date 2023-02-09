# HKO Reverse Engineering Project

Join us on Discord! https://discord.gg/7Yh52hU2NE

***
## About
This is the main repo for the Hello Kitty Online Server Project. It includes the Server the Launcher and a Resource Extractor.
***

## Joining the Public Server
If you want to join the public server, look no further! The instructions below will help you get set up. If you want to set up a local server for testing/development, jump to [Setting up dev enviroment](#setting-up-dev-enviroment)

### Install using the launcher (Recommended)
Download and run the latest [launcher](https://github.com/HelloKittyOnline/HKO-re/releases/latest) application. Continue with [Logging in](#logging-in)

### Manual install
1) Install [Hello Kitty Online](https://archive.org/details/pod-19902-setup) like normal, accepting all of the standard install options. \
2) Install [Clean Flash Player](https://gitlab.com/cleanflash/installer/-/releases). The minigames require having a Flash Player installed. Accept **all** the standard options. \
3) After Hello Kitty is Installed, start notepad or any other text editor as administrator and open `C:\Program Files (x86)\SanrioTown\Hello Kitty Online\Leading.ini`
4) Change the text to say `http://hko.evidentfla.me:8080/single/leading.txt` \
5) Run Hello Kitty Online from your Desktop or Start Menu. 
6) It will now download and update Hello Kitty Online with the latest maps and files. Ignore the 403 error as that is normal. This will take a long time as HKO itself throttles the download speed, not the server. \
7) Once all of the files have finished downloading, Click `Start Game` and you'll be at the login screen! `Patch Now` does not do anything.

### Logging in
Navigate to https://hko.evidentfla.me in your web browser, sign in with your Discord account, and set your username and password.
Login with the username and password you just created
Select `World 1` and enter the game!
Have Fun!

If you have any issues:

1) Check the [Issues](https://github.com/HelloKittyOnline/HKO-re/issues) tab! We may already know of the issue and are working on it!
2) Check the [Discord Server](https://discord.gg/7Yh52hU2NE)! We are willing to help figure out any issues you may have in the #bugs channel.
3) Open an Issue!

***

## Setting up dev enviroment
The Server setup is quite complicated and individual steps often change. \
Join the discord server and ask in #general-dev for setup help.
