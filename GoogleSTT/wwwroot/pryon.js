var token = {};
var socket = {};

//CREDIT: https://addpipe.com/blog/using-recorder-js-to-capture-wav-audio-in-your-html5-web-site/

//webkitURL is deprecated but nevertheless
URL = window.URL || window.webkitURL;

var gumStream; //stream from getUserMedia()
var rec; //Recorder.js object
var input; //MediaStreamAudioSourceNode we'll be recording

// shim for AudioContext when it's not avb. 
var AudioContext = window.AudioContext || window.webkitAudioContext;
var audioContext = new AudioContext; //new audio context to help us record

var recordButton = document.getElementById("recordButton");
var stopButton = document.getElementById("stopButton");
var pauseButton = document.getElementById("pauseButton");
var closeButton = document.getElementById("closeWS");
var list = document.getElementById("messages");

//add events to those 3 buttons
recordButton.addEventListener("click", startRecording);
stopButton.addEventListener("click", stopRecording);
pauseButton.addEventListener("click", pauseRecording);


$(document).ready(function() {
    console.log("");

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

            setTimeout(() => { websocketConnect(); }, 100);
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
    socket = new WebSocket(uri, ["ws","wss","https"]);

    socket.onopen = function (event) {
        console.log("opened connection to " + uri);
    };

    socket.onclose = function (event) {
        console.log("closed connection from " + uri);
    };

    socket.onmessage = function (event) {
        var message = event.data;

        if (message.indexOf("SOCKETID:") > -1) {
            socketId = message.replace("SOCKETID:", "");
            console.log("Socket SUCCESS:", socketId);
            message = "Connected to socketId: " + socketId;
        }

        appendItem(list, message);
        console.log(message);
    };
    socket.onerror = function (event) {
        console.log("error: " + event.data);
    };
    console.log("done...", socket);
}

function startRecording() {
    console.log("recordButton clicked");

    /*
    Simple constraints object, for more advanced audio features see
    <div class="video-container"><blockquote class="wp-embedded-content" data-secret="vNsz0nPBL4"><a href="https://addpipe.com/blog/audio-constraints-getusermedia/">Supported Audio Constraints in getUserMedia()</a></blockquote><iframe class="wp-embedded-content" sandbox="allow-scripts" security="restricted" style="position: absolute; clip: rect(1px, 1px, 1px, 1px);" src="https://addpipe.com/blog/audio-constraints-getusermedia/embed/#?secret=vNsz0nPBL4" data-secret="vNsz0nPBL4" width="600" height="338" title="“Supported Audio Constraints in getUserMedia()” — Pipe Blog" frameborder="0" marginwidth="0" marginheight="0" scrolling="no"></iframe></div>
    */

    var constraints = { audio: true, video: false }

    /*
    Disable the record button until we get a success or fail from getUserMedia()
    */

    recordButton.disabled = true;
    stopButton.disabled = false;
    pauseButton.disabled = false;
    closeButton.disabled = false;

    /*
    We're using the standard promise based getUserMedia()
    https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia
    */

    navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
        console.log("getUserMedia() success, stream created, initializing Recorder.js ...");

        /* assign to gumStream for later use */
        gumStream = stream;

        /* use the stream */
        input = audioContext.createMediaStreamSource(stream);

        /* 
        Create the Recorder object and configure to record mono sound (1 channel)
        Recording 2 channels  will double the file size
        */
        rec = new Recorder(input, { numChannels: 1 })

        //start the recording process
        rec.record()

        console.log("Recording started");

    }).catch(function (err) {
        //enable the record button if getUserMedia() fails
        recordButton.disabled = false;
        stopButton.disabled = true;
        pauseButton.disabled = true
    });
}
function pauseRecording() {
    console.log("pauseButton clicked rec.recording=", rec.recording);
    if (rec.recording) {
        //pause
        rec.stop();
        pauseButton.innerHTML = "Resume";
    } else {
        //resume
        rec.record()
        pauseButton.innerHTML = "Pause";
    }
}
function stopRecording() {
    console.log("stopButton clicked");

    //disable the stop button, enable the record too allow for new recordings
    stopButton.disabled = true;
    recordButton.disabled = false;
    pauseButton.disabled = true;

    //reset button just in case the recording is stopped while paused
    pauseButton.innerHTML = "Pause";

    //tell the recorder to stop the recording
    rec.stop();

    //stop microphone access
    gumStream.getAudioTracks()[0].stop();

    //create the wav blob and pass it on to createDownloadLink
    rec.exportWAV(createDownloadLink);
}

function createDownloadLink(blob) {

    var url = URL.createObjectURL(blob);
    var au = document.createElement('audio');
    var li = document.createElement('li');
    var link = document.createElement('a');
    var uploadUri = "https://" + window.location.host + "/api/upload/uploadfile";

    //console.log("Uploading audio: ", blob);

    //add controls to the <audio> element
    au.controls = true;
    au.src = url;

    //link the a element to the blob
    link.href = url;
    link.download = new Date().toISOString() + '.wav';
    link.innerHTML = link.download;

    //add the new audio and a elements to the li element
    li.appendChild(au);
    li.appendChild(link);

    var filename = new Date().toISOString(); //filename to send to server without extension
    //upload link
    var upload = document.createElement('a');
    upload.href = "#";
    upload.innerHTML = "Upload";
    upload.addEventListener("click", function (event) {
        sendToGoogle(blob);
    })
    li.appendChild(document.createTextNode(" "))//add a space in between
    li.appendChild(upload)//add the upload link to li

    //add the li element to the ordered list
    recordingsList.appendChild(li);
}

function appendItem(list, message) {
    var item = document.createElement("li");
    item.appendChild(document.createTextNode(message));
    list.appendChild(item);
}