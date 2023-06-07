﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class contains the gameobjects and methods for interacting
/// with the UI.
/// </summary>
public class BotUI : MonoBehaviour {
    public GameObject   contentDisplayObject;       // Text gameobject where all the conversation is shown
    public InputField   input;                      // InputField gameobject wher user types their message

    public GameObject   userBubble;                 // reference to user chat bubble prefab
    public GameObject   botBubble;                  // reference to bot chat bubble prefab

    private const int messagePadding = 15;          // space between chat bubbles 
    private int allMessagesHeight = messagePadding;     // int to keep track of where next message should be rendered
    public bool increaseContentObjectHeight;        // bool to check if content object height should be increased

    public NetworkManager networkManager;           // reference to Network Manager script

    /// <summary>
    /// This method is used to update the display text object with the user's and bot's messages.
    /// </summary>
    /// <param name="sender">The one who wrote this message</param>
    /// <param name="message">The message</param>
    public void UpdateDisplay (string sender,  string messageType, string message) {
        // Create chat bubble and add components
        GameObject chatBubbleChild = CreateChatBubble(sender);
        chatBubbleChild.transform.parent.gameObject.SetActive(false);
        StartCoroutine(AddChatComponentAfterDelay(sender, chatBubbleChild, message, messageType));
        
    }

    private IEnumerator AddChatComponentAfterDelay (string sender, GameObject chatBubbleChild, string message, string messageType) {

        Debug.Log(message);
        // Set chat bubble position

        yield return new WaitForSeconds(0.5f);
        
        // Show component after a short animation
        AddChatComponent(chatBubbleChild, message, messageType);
        //chatBubbleChild.SetActive(true);
        // Set focus on input field
        StartCoroutine(SetChatBubblePosition(chatBubbleChild.transform.parent.GetComponent<RectTransform>(), sender));
        chatBubbleChild.transform.parent.gameObject.SetActive(true);
        input.Select();
        input.ActivateInputField();
    }

    /// <summary>
    /// Coroutine to set the position of the chat bubble inside the contentDisplayObject.
    /// </summary>
    /// <param name="chatBubblePos">RectTransform of chat bubble</param>
    /// <param name="sender">Sender who sent the message</param>
    private IEnumerator SetChatBubblePosition (RectTransform chatBubblePos, string sender) {
        // Wait for end of frame before calculating UI transform
        yield return new WaitForEndOfFrame();

        // get horizontal position based on sender
        int horizontalPos = 0;
        if (sender == "Doku") {
            horizontalPos = -20;
        } else if (sender == "Bot") {
            horizontalPos = 20;
        }
        
        // set the chat bubble in correct place
        allMessagesHeight += 15 + (int)chatBubblePos.sizeDelta.y;
        chatBubblePos.anchoredPosition3D = new Vector3(horizontalPos, -allMessagesHeight, 0);

        if (allMessagesHeight > 340)
        {
            // update contentDisplayObject hieght
            RectTransform contentRect = contentDisplayObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, allMessagesHeight + messagePadding);
            contentDisplayObject.transform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
        }
    }

    /// <summary>
    /// Coroutine to update chat bubble positions based on their size.
    /// </summary>
    public IEnumerator RefreshChatBubblePosition () {
        // Wait for end of frame before calculating UI transform
        yield return new WaitForEndOfFrame();
        
        // refresh position of all gameobjects based on size
        int localAllMessagesHeight = messagePadding;
        foreach (RectTransform chatBubbleRect in contentDisplayObject.GetComponent<RectTransform>()) {
            if (chatBubbleRect.sizeDelta.y < 35) {
                localAllMessagesHeight += 35 + messagePadding;
            } else {
                localAllMessagesHeight += (int)chatBubbleRect.sizeDelta.y + messagePadding;
            }
            chatBubbleRect.anchoredPosition3D =
                    new Vector3(chatBubbleRect.anchoredPosition3D.x, -localAllMessagesHeight, 0);
        }

        // Update global message Height variable
        allMessagesHeight = localAllMessagesHeight;
        if (allMessagesHeight > 340) {
            // update contentDisplayObject hieght
            RectTransform contentRect = contentDisplayObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, allMessagesHeight + messagePadding);
            contentDisplayObject.transform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
        }
    }

    /// <summary>
    /// This method creates chat bubbles from prefabs and sets their positions.
    /// </summary>
    /// <param name="sender">The sender of message for which bubble is rendered</param>
    /// <returns>Reference to empty gameobject on which message components can be added</returns>
    private GameObject CreateChatBubble (string sender) {
        GameObject chat = null;
        if (sender == "Doku") {
            // Create user chat bubble from prefabs and set it's position
            chat = Instantiate(userBubble);
            chat.transform.SetParent(contentDisplayObject.transform, false);
        } else if (sender == "Bot") {
            // Create bot chat bubble from prefabs and set it's position
            chat = Instantiate(botBubble);
            chat.transform.SetParent(contentDisplayObject.transform, false);
        }
        // Add vertical layout group
        VerticalLayoutGroup verticalLayout = chat.AddComponent<VerticalLayoutGroup>();
        if (sender == "Doku") {
            verticalLayout.padding = new RectOffset(10, 20, 5, 5);
        } else if (sender == "Bot") {
            verticalLayout.padding = new RectOffset(20, 10, 5, 5);
        }
        verticalLayout.childAlignment = TextAnchor.MiddleCenter;

        // Return empty gameobject on which chat components will be added
        return chat.transform.GetChild(0).gameObject;
    }

    /// <summary>
    /// This method adds message component to chat bubbles based on message type.
    /// </summary>
    /// <param name="chatBubbleObject">The empty gameobject under chat bubble</param>
    /// <param name="message">message to be shown</param>
    /// <param name="messageType">The type of message (text, image etc)</param>
    private void AddChatComponent (GameObject chatBubbleObject, string message, string messageType) {

        switch (messageType) {
            case "text":


                // Create and init Text component
                
                Text chatMessage = chatBubbleObject.AddComponent<Text>();
                chatMessage.font = Resources.Load<Font>("ARIAL");
                //chatMessage.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

                ContentSizeFitter chatSize = chatBubbleObject.transform.parent.gameObject.AddComponent<ContentSizeFitter>();
                if (message.Length > 25)
                {
                    chatSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
                else
                {
                    chatSize.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                chatSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                //chatBubbleObject.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 0f);
                //float preferredWidth = chatBubbleObject.GetComponent<Text>().preferredWidth;
                //if (preferredWidth > 800f)
                //{
                //    preferredWidth = 800f;
                //}
                //chatBubbleObject.GetComponent<RectTransform>().sizeDelta = new Vector2(preferredWidth, chatBubbleObject.GetComponent<Text>().preferredHeight);
                chatMessage.fontSize = 18;
                chatMessage.alignment = TextAnchor.MiddleLeft;
                chatMessage.text = message;
                
                break;
            case "image":
                // Create and init Image component
                Image chatImage = chatBubbleObject.AddComponent<Image>();
                StartCoroutine(networkManager.SetImageTextureFromUrl(message, chatImage));
                break;
            case "attachment":
                break;
            case "buttons":
                break;
            case "elements":
                break;
            case "quick_replies":
                break;
        }
    }
}
