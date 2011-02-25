var net = require('net');
var dataToSend;
var socketServer;
net.createServer(function (socket)
{
  socketServer = socket;
  socket.write("Server Initialized.\r\n");
  
  setInterval ( writeToSocketServer, 33 );
  socket.on("data", function (data)
  {
    dataToSend = data
    //socket.write(data);
  });
}).listen(8124, "127.0.0.1");
console.log('Server running at http://127.0.0.1:8124/');

function writeToSocketServer()
{    
    if (dataToSend) 
    {
        console.log('[node] '+dataToSend);
        socketServer.write(dataToSend);
    }
}
