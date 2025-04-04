// <copyright file="NetworkConnection.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>

// <authors> Moon Jang & Arturo Bacon</authors>
// <date> March 27, 2025 </date>
using System.Net.Sockets;
using System.Text;
namespace CS3500.Networking;

/// <summary>
///   Wraps the StreamReader/Writer/TcpClient together so we
///   don't have to keep creating all three for network actions.
/// </summary>
public sealed class NetworkConnection : IDisposable
{
    /// <summary>
    ///   The connection/socket abstraction
    /// </summary>
    private TcpClient _tcpClient = new();

    /// <summary>
    ///   Reading end of the connection
    /// </summary>
    private StreamReader? _reader = null;

    /// <summary>
    ///   Writing end of the connection
    /// </summary>
    private StreamWriter? _writer = null;

    /// <summary>
    ///   Initializes a new instance of the <see cref="NetworkConnection"/> class.
    ///   <para>
    ///     Create a network connection object.
    ///   </para>
    /// </summary>
    /// <param name="tcpClient">
    ///   An already existing TcpClient
    /// </param>
    public NetworkConnection( TcpClient tcpClient )
    {
        _tcpClient = tcpClient;
        if ( IsConnected )
        {
            // Only establish the reader/writer if the provided TcpClient is already connected.
            _reader = new StreamReader( _tcpClient.GetStream(), Encoding.UTF8 );
            _writer = new StreamWriter( _tcpClient.GetStream(), Encoding.UTF8 ) { AutoFlush = true }; // AutoFlush ensures data is sent immediately
        }
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="NetworkConnection"/> class.
    ///   <para>
    ///     Create a network connection object.  The tcpClient will be unconnected at the start.
    ///   </para>
    /// </summary>
    public NetworkConnection( )
        : this( new TcpClient( ) )
    {
    }

    /// <summary>
    /// Gets a value indicating whether the socket is connected.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            try
            {
                // We use the null-conditional operator ?. in case _tcpClient is null.
                return _tcpClient?.Connected == true;
            }
            catch
            {
                return false;
            }
        }
    }


    /// <summary>
    ///   Try to connect to the given host:port. 
    /// </summary>
    /// <param name="host"> The URL or IP address, e.g., www.cs.utah.edu, or  127.0.0.1. </param>
    /// <param name="port"> The port, e.g., 11000. </param>
    public void Connect( string host, int port )
    {
        // This method is responsible for establishing the connection to the server and setting up communication.
        if (IsConnected)
        {
            throw new InvalidOperationException("Already connected.");
        }

        try
        {
            // This line attempts to connect to the server using the specified host and port
            _tcpClient.Connect(host, port);

            // we set up a reader and writer so we can send and receive messages.
            _reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8);
            _writer = new StreamWriter(_tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Connection failed.");
        }
    }


    /// <summary>
    ///   Send a message to the remote server.  If the <paramref name="message"/> contains
    ///   new lines, these will be treated on the receiving side as multiple messages.
    ///   This method should attach a newline to the end of the <paramref name="message"/>
    ///   (by using WriteLine).
    ///   If this operation can not be completed (e.g. because this NetworkConnection is not
    ///   connected), throw an InvalidOperationException.
    /// </summary>
    /// <param name="message"> The string of characters to send. </param>
    public void Send( string message )
    {
        if (!IsConnected || _writer == null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        try
        {
            _writer.WriteLine(message);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Failed to send message.");
        }
    }


    /// <summary>
    ///   Read a message from the remote side of the connection.  The message will contain
    ///   all characters up to the first new line. See <see cref="Send"/>.
    ///   If this operation can not be completed (e.g. because this NetworkConnection is not
    ///   connected), throw an InvalidOperationException.
    /// </summary>
    /// <returns> The contents of the message. </returns>
    public string ReadLine( )
    {
        if (!IsConnected || _reader == null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        try
        {
            string? message = _reader.ReadLine();
            if (message == null)
            {
                throw new InvalidOperationException("Connection was closed.");
            }

            return message;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Failed to read message." );
        }

    }

    /// <summary>
    ///   If connected, disconnect the connection and clean 
    ///   up (dispose) any streams.
    /// </summary>
    public void Disconnect( )
    {
        try
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close(); // or .Dispose()
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during disconnect: " + ex.Message);
        }
    }

    /// <summary>
    ///   Automatically called with a using statement (see IDisposable)
    /// </summary>
    public void Dispose( )
    {
        Disconnect();
    }
}
