# SharpPuppet
SharpPuppet gives an operator the ability to choose an open window and injects scriptable keystrokes into that window. The idea is to activate target GUI windows and then emulate a keyboard to inject keystrokes into the active window.

To achieve this I took a look at on screen keyboards for windows and discovered how they were doing keystrokes. SharpPuppet has the ability to use windows native API to inject keystrokes and also using C# built in SendKeys(). During my research I found that using SendKeys() did not work when attempting to type into some open windows, but windows native APIs did. 

Looking at implementations of windows on-screen keyboards I was able to piece together a working tool that could send keystrokes into any open window (it has worked on everything I have tried so far).  One of the most helpful things for building this tool was this C# on-screen keyboard:
* https://github.com/LeonidMTertitski/osk-on-screen-keyboard-windows

SharpPuppet takes a process name or PID and a string with ducky script like commands and then activates the chosen process open window and executes the commands.  SharpPuppet also supports base64 encoded strings for loading into the targets clipboard buffer and then pasting into an open window of choice.

SharpPuppet comes with some situational awareness built in. For instance, using the lockstatus command will tell you if the victims computer open or locked (some false positives).  SharpPuppet also has the ability to read the title of the open windows that is targeted.  For a Putty session this window title most often includes the bash prompt with the SSH server hostname and current directory.

## Use Cases
* Executing commands in open Putty session
* Executing commands in open RDP session
* Typing into an open application
* Other stuff (insert imagination here)

## Modified Ducky Script
* MAX: Activates and opens the window to its maximum 
* MIN: Minimizes window
* DELAY: Delay specified in ms
* FULL: Makes windows such as RDP Go full-screen (needed to inject the LWIN key)
* GUI r: LWIN + r = run box
* NORM: Open window to its normal size
* STRING: String followed by a string of characters to be injected ( Uses Windows Native Api Calls )
* VSTRING:("virtual" string) followed by a string of characters to be injected ( uses the SendKeys() function, lets you type in some applications with them minimized eg putty)
* PASTEPAYLOAD: Loads a base64 encoded payload script into clipboard buffer and pastes it into active window
* ENTER: The Enter key


## Examples
### Running Whoami in an open putty window
* .\SharpPuppet.exe putty "NORM\~\~DELAY 200\~\~STRING whoami\~\~DELAY 100\~\~ENTER"
* FROM COBALT-STRIKE: execute-assembly \<path\>/SharpPuppet.exe putty "NORM\~\~DELAY 200\~\~STRING whoami\~\~DELAY 100\~\~ENTER"

### Popping Calc on open RDP session
* .\SharpPuppet.exe mstsc 'FULL\~\~DELAY 100\~\~GUI r\~\~DELAY 100\~\~STRING ftp.exe\~\~DELAY 100\~\~ENTER\~\~DELAY 500\~\~PASTEPAYLOAD\~\~DELAY 200\~\~MIN' 'IXBvd2Vyc2hlbGwuZXhlIC13IGhpZGRlbiAtbm9wIC1jIGNhbGMuZXhl'
* FROM COBALT-STRIKE: execute-assembly \<path\>/SharpPuppet.exe mstsc 'FULL\~\~DELAY 100\~\~GUI r\~\~DELAY 100\~\~STRING ftp.exe\~\~DELAY 100\~\~ENTER\~\~DELAY 500\~\~PASTEPAYLOAD\~\~DELAY 200\~\~MIN' 'IXBvd2Vyc2hlbGwuZXhlIC13IGhpZGRlbiAtbm9wIC1jIGNhbGMuZXhl'

## Resources
### Ducky Script Command Reference
* https://docs.hak5.org/hc/en-us/articles/360049449314-Ducky-Script-Command-Reference#:~:text=Ducky%20Script%20is%20the%20payload,for%20all%20Hak5%20payload%20platforms

### Cobalt Strike In-Memory .Net Assembly Execution
* https://blog.cobaltstrike.com/2018/04/09/cobalt-strike-3-11-the-snake-that-eats-its-tail/

## Acknowledgments
@Th3M00se
