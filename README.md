# GoogleSTT
Sample code for sending mic audio from webpage to Google Cloud Speech to Text API using the Google SDK for .Net

## First prize
Stream audio from mic to Google Speech to Text proxied through a backend

## Second prize
Send full audio file from web page to backend to Google STT

## Status

* Successfully achieved the `Second prize` (branch_Record-in-browser-before-sending-all | index.html)
 1. Capture audio and buffer in page
 2. Send WAV from web page to backend via POST
 3. Send WAV from backend to Google STT
 4. Handle the responses and send back via websocket.

* Working on `First prize` (branch_Record-in-browser-before-sending-all | mediaStreamRecorder.html)
 1. Capturing audio in 1 sec intervals
 2. Sending WAV chunks to backend via POST
 3. Save chucks to disk and ensure are playable WAV files for each chunck
 4. Send each WAV chunk to Google STT
 5. No response (400 error)

## TODO
* Achieve `First prize`
* Clean up code
* Document code and update README
* merge to master
* Answer https://stackoverflow.com/questions/52518467/streaming-audio-buffer-from-a-web-page-to-c-sharp-google-cloud-speech-to-text-sd
