///
var token = {};
var socket = {};
var streamer = {};

//CREDIT: https://addpipe.com/blog/using-recorder-js-to-capture-wav-audio-in-your-html5-web-site/

//webkitURL is deprecated but nevertheless
URL = window.URL || window.webkitURL;

var gumStream; //stream from getUserMedia()
var rec; //Recorder.js object
var input; //MediaStreamAudioSourceNode we'll be recording

// shim for AudioContext when it's not avb. 
var AudioContext = window.AudioContext || window.webkitAudioContext;
var audioContext = new AudioContext; //new audio context to help us record

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


$(document).ready(function() {
    console.log("Document Ready");
    var context = new AudioContext();
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
                createStreamer();
            }, 100);
        },
        error: function(error) {
            alert("Failed: " + JSON.stringify(error));
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
function createStreamer() {
    var uri = token.webSocketUrl + token.access_token;

    var defaultConfig = {
        codec: {
            sampleRate: 1600,
            channels: 1,
            app: 2049,
            frameDuration: 10,
            bufferSize: 3072
        },
        server: {
            host: uri
        }
    };

    streamer = new WSAudioAPI.Streamer(defaultConfig, socket);
}
function startExchange() {
    var startMessage = {
        "action": "START",
        "exchange_request": {
            "input": {
                "raw_text": "what is my net worth"
            }
        }
    };

    console.log("startMessage", startMessage);

    socket.send(JSON.stringify(startMessage));
}

function startRecording() {
    console.log("recordButton clicked");

    try {
        streamer.start();
    } catch (e) {
        console.error(e);
        //enable the record button if getUserMedia() fails
        recordButton.disabled = false;
        stopButton.disabled = true;
        pauseButton.disabled = true;
    }
}
function pauseRecording() {
    console.log("pauseButton clicked rec.recording=", rec.recording);
    streamer.mute();
}
function stopRecording() {
    console.log("stopButton clicked");

    //disable the stop button, enable the record too allow for new recordings
    stopButton.disabled = true;
    recordButton.disabled = false;
    pauseButton.disabled = true;

    //reset button just in case the recording is stopped while paused
    pauseButton.innerHTML = "Pause";

    streamer.stop();
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