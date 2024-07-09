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
public class MessageManager : MonoBehaviour
{
    private string previousMessage;

    // Start is called before the first frame update
    void Start()
    {
		 previousMessage = "";
    }


    // Update is called once per frame
    void Update()
    {
		StringHandler handler = gameObject.GetComponent<StringHandler>();
        if (handler.lastMessage != previousMessage){
			previousMessage = handler.lastMessage;
			
			if (handler.lastMessage.Trim().StartsWith("/say ")){
				DialogManager dm = gameObject.GetComponent<DialogManager>();
				dm.PlayAudio(handler.lastMessage.Replace("/say ", ""));
			}
		}
    }
}
