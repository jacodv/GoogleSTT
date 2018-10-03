        var list = document.getElementById("messages");
        var uri = "wss://" + window.location.host + "/audiows";
        var socket;
        var socketId = null;

        console.log("AudioWS: " + uri);
        function websocketConnect() {
            console.log("connecting...")
            socket = new WebSocket(uri);
            //socket.binaryType = 'arraybuffer';
            socket.onopen = function (event) {
                console.log("opened connection to " + uri);
            };
            socket.onclose = function (event) {
                console.log("closed connection from " + uri);
            };
            socket.onmessage = function (event) {
                var message = event.data;
                if(message.indexOf("SOCKETID:")>-1)
                {
                    socketId = message.replace("SOCKETID:","");
                    console.log("Socket SUCCESS:",socketId);
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
        function websocketStatus() {
            console.log("status...", socket);
        }
        function websocketSendMessage(message) {
            socket.send(message);
        }
        function websocketClose()
        {
            if (socket) {
                setTimeout(function(){console.log("closing...", socket);socket.close();}, 1000);
            }
        }
        function appendItem(list, message) {
            var item = document.createElement("li");
            item.appendChild(document.createTextNode(message));
            list.appendChild(item);
        }    
