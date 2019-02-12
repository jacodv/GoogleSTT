window.AudioContext = window.AudioContext || window.webkitAudioContext;


function AudioStreamer(statusCallBack, socket) {
    var self = this;

    self.myStream;
    self.scriptProcessor;
    self.audioContext;

    self.statusCallBack = statusCallBack;
    self.socket = socket;

    self.canRecord = false;

    self.recordedBuffers = [];

    self.init = function () {
        navigator.mediaDevices.getUserMedia({
                audio: {
                    mandatory: {
                        googEchoCancellation: 'false',
                        googAutoGainControl: 'false',
                        googNoiseSuppression: 'false',
                        googHighpassFilter: 'false',
                    },
                },
            }).then((stream, callback)=>{
                self.audioContext = self.audioContext || new AudioContext();
                if (!self.audioContext) {
                    return;
                }
                self.myStream = stream;
                self.statusCallBack("AudioStreamer initialized");
                self.canRecord = true;
            })
            .catch(e => {
                /* If there are some errors with parameter configurations or 
                user didn’t give you the access to the microphone inside the browser, you end here. */
                console.log(e);
                self.statusCallBack("AudioStreamer initialization failed");
                self.canRecord = false;
                console.error(e);
            });
    };

    self.startRecording = function(){
        if(!self.canRecord){
            self.statusCallBack("Cannot start recording");
            console.error("Cannot start recording: canRecord=false, call init()");
            return;
        }

        const inputPoint = self.audioContext.createGain();
        const microphone = self.audioContext.createMediaStreamSource(self.myStream);
        const analyser = self.audioContext.createAnalyser();
        self.scriptProcessor = inputPoint.context.createScriptProcessor(2048, 2, 2);
    
        microphone.connect(inputPoint);
        inputPoint.connect(analyser);
        inputPoint.connect(self.scriptProcessor);
        self.scriptProcessor.connect(inputPoint.context.destination);
        // This is for registering to the “data” event of audio stream, without overwriting the default scriptProcessor.onAudioProcess function if there is one.
        self.scriptProcessor.addEventListener('audioprocess', streamAudioData);
    
    };

    self.stopRecording = function () {
        self.canRecord = false;
        self.statusCallBack("Stopping recording");
        if (self.myStream) {
            // stop the browser microphone
            self.myStream.getTracks()[0].stop();
            self.myStream = null;
        }

        if (self.scriptProcessor) {
            // Stop listening the stream from the michrophone
            self.scriptProcessor.removeEventListener('audioprocess', streamAudioData);
        }
        self.statusCallBack("Recording stopeed");
    };

    self.createBlob = function()
    {

    }
};

const ConversionFactor = 2 ** (16 - 1) - 1; // 32767
const MAX_INT = Number.MIN_SAFE_INTEGER;
const streamAudioData = e => {
    const floatSamples = e.inputBuffer.getChannelData(0);

    ////
    var convertedData = Int16Array.from(floatSamples.map(n => n * MAX_INT));
    recordedBuffers.push(convertedData);
    console.log("Audio data: ", convertedData)

    if (socket && socket.readyState === socket.OPEN) {
        socket.send(convertedData);
    }
};