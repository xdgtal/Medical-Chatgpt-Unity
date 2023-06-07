using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEditor.VersionControl;
using TreeEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using static UnityEditor.Timeline.TimelinePlaybackControls;

/// <summary>
/// This class handles all the requests and serialization and
/// deserialization of data.
/// </summary>
public class NetworkManager : MonoBehaviour {
    // reference to the BotUI class
    public BotUI            botUI;
    // the url at which the bot's custom component is hosted
    private const string    rasa_url = "http://127.0.0.1:5005/webhooks/unity/webhook";

    // Your OpenAI API key
    private string apiKey = "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";

    // The prompt that you want to complete
    private string model = "gpt-3.5-turbo";

    // The maximum number of tokens to generate in the completion
    private int maxTokens = 500;

    // The temperature of the sampling distribution to use for generating completions
    // A higher temperature will result in more random completions, while a lower temperature will be more conservative
    private float temperature = 0.5f;

    // The URL for the OpenAI Chat Completion API endpoint
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    IEnumerator GenerateCompletion(string jsonBody)
    {

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        byte[] rawBody = new System.Text.UTF8Encoding().GetBytes(jsonBody);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(rawBody);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // recieve the response asynchronously
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            JObject resJson = JObject.Parse(responseText); 
            var credsJsonRaw = resJson["choices"];
            //JArray credsJson = JArray.Parse(credsJsonRaw);
            foreach (JObject obj in credsJsonRaw)
            {
                var messageJson = obj["message"];
                var content = messageJson["content"].Value<string>();

                Debug.Log(content);
                botUI.UpdateDisplay("Bot", "text", content);
            }
            //Console.WriteLine(jsonObject);
            // Get the value of the first choice as a string
            Debug.Log(responseText);
        }
        else
        {
            Debug.Log(request.error);
        }
        // Check for errors
        // Get the response text and log it
    }


    /// <summary>
    /// This method is called when user has entered their message and hits
    /// the send button. It calls the <see cref="NetworkManager.PostRequest"/> coroutine
    /// to send the user message to bot and also updates UI with the users message.
    /// </summary>
    public void SendMessage () {
        // Get message from textbox and clear the input field
        string message = botUI.input.text;
        botUI.input.text = "";
        botUI.UpdateDisplay("Doku", "text", message);
        ChatMessage[] messages = new ChatMessage[2];
        messages[0] = new ChatMessage { role = "system", content = "You are a medical bot" };
        messages[1] = new ChatMessage { role = "user", content = message };
        // Create a json object from user message
        PostData postMessage = new PostData {
            messages = messages,
            max_tokens = maxTokens,
            temperature = temperature,
            model = model
        };


        //string jsonBody = JsonUtKCility.ToJson(postMessage);
        string jsonBody = JsonConvert.SerializeObject(postMessage);

        // Update display
        // botUI.UpdateDisplay("Doku", message, "text");

        // Create a post request with the data to send to Rasa server
        //StartCoroutine(PostRequest(rasa_url, jsonBody));

        StartCoroutine(GenerateCompletion(jsonBody));
    }

    /// <summary>
    /// This method updates the UI with the bot's response.
    /// </summary>
    /// <param name="response">The response json recieved from the bot</param>
    public void RecieveMessage (string response) {
        // Deserialize response recieved from the bot
        RecieveData recieveMessages =
            JsonUtility.FromJson<RecieveData>(response);

        // show message based on message type on UI
    }

    /// <summary>
    /// This is a coroutine to asynchronously hit the server url with users message
    /// wrapped in request. The response is deserialized and rendered on the UI
    /// </summary>
    /// <param name="url">The url where the rasa server's custom connector is located</param>
    /// <param name="jsonBody">User message serialized into a json object</param>
    /// <returns></returns>
    private IEnumerator PostRequest (string url, string jsonBody) {
        // Create a request to hit the rasa custom connector
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] rawBody = new System.Text.UTF8Encoding().GetBytes(jsonBody);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(rawBody);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        // recieve the response asynchronously
        yield return request.SendWebRequest();
        
        // Show response on UI
        RecieveMessage(request.downloadHandler.text);
    }

    /// <summary>
    /// This method gets url resource from link and applies it to the passed texture.
    /// </summary>
    /// <param name="url">url where the image resource is located</param>
    /// <param name="image">RawImage object on which the texture will be applied</param>
    /// <returns></returns>
    public IEnumerator SetImageTextureFromUrl (string url, Image image) {
        // Send request to get the image resource
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        if (request.isNetworkError || request.isHttpError)
            // image could not be retrieved
            Debug.Log(request.error);
        
        else {
            // Create Texture2D from Texture object
            Texture texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            Texture2D texture2D = texture.ToTexture2D();

            // set max size for image width and height based on chat size limits
            float imageWidth = 0, imageHeight = 0, texWidth = texture2D.width, texHeight = texture2D.height;
            if ((texture2D.width > texture2D.height) && texHeight > 0) {
                // Landscape image
                imageWidth = texWidth;
                if (imageWidth > 200) imageWidth = 200;
                float ratio = texWidth / imageWidth;
                imageHeight = texHeight / ratio;
            }
            if ((texture2D.width < texture2D.height) && texWidth > 0) {
                // Portrait image
                imageHeight = texHeight;
                if (imageHeight > 200) imageHeight = 200;
                float ratio = texHeight / imageHeight;
                imageWidth = texWidth/ ratio;
            }

            // Resize texture to chat size limits and attach to message 
            // Image object as sprite
            TextureScale.Bilinear(texture2D, (int)imageWidth, (int)imageHeight);
            image.sprite = Sprite.Create(
                texture2D, 
                new Rect(0.0f, 0.0f, texture2D.width, texture2D.height), 
                new Vector2(0.5f, 0.5f), 100.0f);

            // Resize and reposition all chat bubbles
            StartCoroutine(botUI.RefreshChatBubblePosition());
        }
    }
}
