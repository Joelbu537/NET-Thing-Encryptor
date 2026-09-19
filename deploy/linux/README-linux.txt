NET Thing Encryptor for Linux
=============================

This build is self-contained and does not require a separate .NET runtime.
Audio and video playback require the system LibVLC packages. On Debian and
Ubuntu install them with:

  sudo apt install libvlc5 libvlc-dev vlc-plugin-base vlc-plugin-video-output

Start the application with:

  ./NET\ Thing\ Encryptor

Application data is stored below the current user's standard application data
directory. Removing the application does not remove vault data.
