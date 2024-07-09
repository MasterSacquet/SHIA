using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/*
 * Ce script permet de recevoir sur une socket les émotions détectés par le script Python
 */
public class StringHandler : MonoBehaviour
{
    private TcpListener tcpListener;
    public int port=26950;
    private byte[] receivebuffer;
    private byte[] sendbuffer;
    private TcpClient _client;
    private NetworkStream stream;
    private int bufferSize;
    public string lastMessage;

    // Start is called before the first frame update
    void Start()
    {
        bufferSize = 4096;

        //Ouverture de la socket
        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        Debug.Log("Serveur démarré");
        tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(TCPCallback), null);
    }

    private void TCPCallback(IAsyncResult _result)
    {
        //Connection d'un client (le script python)
        _client = tcpListener.EndAcceptTcpClient(_result);
        Debug.Log("Client connecté");
        _client.ReceiveBufferSize = bufferSize;
        _client.SendBufferSize = bufferSize;
        stream = _client.GetStream();

        receivebuffer = new byte[bufferSize];
        stream.BeginRead(receivebuffer, 0, bufferSize, ReceiveCallBack, null);
    }

    private void ReceiveCallBack(IAsyncResult _result)
    {
        //Réception des messages du client 
        try
        {
            int byteLength = stream.EndRead(_result);
            if (byteLength <= 0)
            {
                return;
            }

            byte[] data = new byte[byteLength];
            Array.Copy(receivebuffer, data, byteLength);

            //Pour l'instant on affiche la chaîne de caractères reçue dans la console de Unity.
            Debug.Log("Data reçu : " + System.Text.Encoding.Default.GetString(receivebuffer));

			lastMessage = System.Text.Encoding.Default.GetString(receivebuffer).Trim();
            //Si besoin de test de connection, renvoit le message sur la même Socket
            //SendString("received: " + System.Text.Encoding.Default.GetString(receivebuffer));

            //Relance la lecture asynchrone
            receivebuffer = new byte[bufferSize];
            stream.BeginRead(receivebuffer, 0, bufferSize, ReceiveCallBack, null);
        }
        catch (Exception ex)
        {
            Debug.Log("Erreur serveur : " + ex.ToString());
        }

    }

    public void SendString(String _message)
    {
        if (_client != null)
        {
            sendbuffer = new byte[bufferSize];
            byte[] tmp = Encoding.UTF8.GetBytes(_message+"\n");

            Array.Copy(tmp, sendbuffer, Math.Min(bufferSize, tmp.Length));

            try
            {
                stream.Write(sendbuffer, 0, bufferSize);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                Debug.Log($"Error: {argumentOutOfRangeException.Message}");
            }
        }
        else
        {
            Debug.Log("Tried to send message {" + _message + "} but connection is closed.");
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
