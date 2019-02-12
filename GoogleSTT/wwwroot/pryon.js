﻿///
var token;
var socket;
var audioStreamer;

var startExchangeButton = document.getElementById("startExchangeButton");
var recordButton = document.getElementById("recordButton");
var stopButton = document.getElementById("stopButton");
var pauseButton = document.getElementById("pauseButton");
var closeButton = document.getElementById("closeWS");
var list = document.getElementById("messages");

//add events to those 3 buttons
startExchangeButton.addEventListener("click", startExchange);
recordButton.addEventListener("click", startRecording);
stopButton.addEventListener("click", stopRecording);
pauseButton.addEventListener("click", pauseRecording);
closeButton.addEventListener("click", websocketClose);

$(document).ready(function() {
    console.log("Document Ready");
    audioStreamer = new AudioStreamer(statusCallback, socket);
    audioStreamer.init();
});

function getAuthToken() {
    //var authUrl = "https://localhost:44391/api/pryon";
    var authUrl = "/api/pryon";

    var request = {
        url: authUrl,
        dataType: "json",
        success: function(data) {
            alert("success");
            token = data;

            $("#token").text(JSON.stringify(data));

            setTimeout(() => {
                websocketConnect();
            }, 100);
        },
        error: function(error) {
            alert("Failed: " + JSON.stringify(error));
            console.error(error);
        }
    };

    console.log("Request:", request);

    $.ajax(request);
}
function websocketConnect() {
    var uri = token.webSocketUrl + token.access_token;

    console.log("connecting... ", uri);
    socket = new WebSocket(uri);

    socket.onopen = function (event) {
        console.log("opened connection to " + uri);
    };

    socket.onclose = function (event) {
        console.log("closed connection from " + uri);
    };

    socket.onmessage = function (event) {
        receiveMessage(event);
    };
    socket.onerror = function (event) {
        console.log("error: " + event.data);
    };
    console.log("done...", socket);
}

function websocketClose()
{
    if(socket)
        socket.close();
}

function startExchange() {
    var startMessage = {
        "action": "START",
        "exchange_request": {
            "raw_audio": {
                "audio_encoding": "LINEAR16",
                "sample_rate_hertz": 16000,
            }
        }
    };

    console.log("startMessage", startMessage);

    socket.send(JSON.stringify(startMessage));

    setTimeout(()=>{self.startRecording();},500);
}

function startRecording() {
    console.log("recordButton clicked");

    try {
        audioStreamer.startRecording();
        recordButton.disabled = true;
        stopButton.disabled = false;
        pauseButton.disabled = false;
    }    
    catch (e) {
        console.error(e);
        //enable the record button if getUserMedia() fails
        recordButton.disabled = false;
        stopButton.disabled = true;
        pauseButton.disabled = true;
    }
}
function pauseRecording() {
    console.error("pauseButton clicked : NOT SUPPORTED");
}
function stopRecording() {
    console.log("stopButton clicked");

    //disable the stop button, enable the record too allow for new recordings
    stopButton.disabled = true;
    recordButton.disabled = false;
    pauseButton.disabled = true;

    //reset button just in case the recording is stopped while paused
    pauseButton.innerHTML = "Pause";

    audioStreamer.stopRecording();
}

function statusCallback(message)
{
    console.log(message);
}

function receiveMessage(event) {
    console.log("Received: ", event.data);

    var message = JSON.parse(event.data);

    console.log("Message Data: ", message);
    appendItem(list, event.data);

    if (message.data.state === "PROCESSING") {
        socket.send(JSON.stringify({"action":"STOP"}));
    }
}
function appendItem(list, message) {
    var item = document.createElement("li");
    item.appendChild(document.createTextNode(message));
    list.appendChild(item);
}