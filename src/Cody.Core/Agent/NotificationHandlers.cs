﻿using Cody.Core.Agent.Protocol;
using EnvDTE80;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Cody.Core.Agent
{
    public class NotificationHandlers : INotificationHandler
    {
        public NotificationHandlers()
        {
        }
        public delegate Task PostWebMessageAsJsonDelegate(string message);
        public PostWebMessageAsJsonDelegate PostWebMessageAsJson { get; set; }

        public event EventHandler<SetHtmlEvent> OnSetHtmlEvent;
        public event EventHandler<AgentResponseEvent> OnPostMessageEvent;

        public IAgentService agentClient;

        private TaskCompletionSource<bool> agentClientReady = new TaskCompletionSource<bool>();


        public void SetAgentClient(IAgentService client)
        {
            this.agentClient = client;
            agentClientReady.SetResult(true);
        }

        // Send a message to the host from webview.
        public async Task SendWebviewMessage(string handle, string message)
        {
            // Turn message into a JSON object
            var json = JObject.Parse(message);
            var command = json["command"]?.ToString();

            switch (command)
            {
                case "links":
                    var link = json["value"]?.ToString();
                    if (!string.IsNullOrEmpty(link))
                    {
                        // if the is links, open the link in the default browser
                        // string link = json["value"].ToString();
                        // System.Diagnostics.Process.Start(link);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true });
                    }
                    return;

                case "command":
                    var id = json["id"]?.ToString();
                    // Open the extension options page for authentication related commands.
                    if (id == "cody.status-bar.interacted" || id.StartsWith("cody.auth.signin") || id.StartsWith("cody.auth.signout"))
                    {
                        try
                        {
                            var dte = (DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
                            dte.ExecuteCommand("Tools.Options", "Cody.General");
                        }
                        catch (System.Runtime.InteropServices.COMException)
                        {
                            // Handle the case where Visual Studio is not running or COM object is not accessible
                        }
                        return;
                    }
                    break;
            }

            await agentClient.ReceiveMessageStringEncoded(new ReceiveMessageStringEncodedParams
            {
                Id = handle,
                MessageStringEncoded = message
            });
        }

        [AgentNotification("debug/message")]
        public void Debug(string channel, string message)
        {
            System.Diagnostics.Debug.WriteLine(message, "Agent Debug");
        }

        [AgentNotification("webview/registerWebview")]
        public void RegisterWebview(string handle)
        {
            System.Diagnostics.Debug.WriteLine(handle, "Agent registerWebview");
        }

        [AgentNotification("webview/registerWebviewViewProvider")]
        public async Task RegisterWebviewViewProvider(string viewId, bool retainContextWhenHidden)
        {
            agentClientReady.Task.Wait();
            await agentClient.ResolveWebviewView(new ResolveWebviewViewParams
            {
                // cody.chat for sidebar view, or cody.editorPanel for editor panel
                ViewId = viewId,
                // TODO: Create dynmically when we support editor panel
                WebviewHandle = "visual-studio-sidebar",
            });
            System.Diagnostics.Debug.WriteLine(viewId, retainContextWhenHidden, "Agent registerWebviewViewProvider");
        }

        [AgentNotification("webview/createWebviewPanel", deserializeToSingleObject: true)]
        public void CreateWebviewPanel(CreateWebviewPanelParams panelParams)
        {
            System.Diagnostics.Debug.WriteLine(panelParams, "Agent createWebviewPanel");
        }

        [AgentNotification("webview/setOptions")]
        public void SetOptions(string handle, DefiniteWebviewOptions options)
        {
            if (options.EnableCommandUris is bool enableCmd)
            {

            }
            else if (options.EnableCommandUris is JArray jArray)
            {
                var uris = jArray.ToObject<string[]>();
            }
        }

        [AgentNotification("webview/setHtml")]
        public void SetHtml(string handle, string html)
        {
            System.Diagnostics.Debug.WriteLine(html, "Agent setHtml");
            OnSetHtmlEvent?.Invoke(this, new SetHtmlEvent() { Handle = handle, Html = html });
        }

        [AgentNotification("webview/PostMessage")]
        public void PostMessage(string handle, string message)
        {
            PostMessageStringEncoded(handle, message);
        }

        [AgentNotification("webview/postMessageStringEncoded")]
        public void PostMessageStringEncoded(string id, string stringEncodedMessage)
        {
            System.Diagnostics.Debug.WriteLine(stringEncodedMessage, "Agent postMessageStringEncoded");
            PostWebMessageAsJson?.Invoke(stringEncodedMessage);
        }

        [AgentNotification("webview/didDisposeNative")]
        public void DidDisposeNative(string handle)
        {

        }

        [AgentNotification("extensionConfiguration/didChange", deserializeToSingleObject: true)]
        public void ExtensionConfigDidChange(ExtensionConfiguration config)
        {
            System.Diagnostics.Debug.WriteLine(config, "Agent didChange");
        }

        [AgentNotification("webview/dispose")]
        public void Dispose(string handle)
        {
            System.Diagnostics.Debug.WriteLine(handle, "Agent dispose");
        }

        [AgentNotification("webview/reveal")]
        public void Reveal(string handle, int viewColumn, bool preserveFocus)
        {
            System.Diagnostics.Debug.WriteLine(handle, "Agent reveal");
        }

        [AgentNotification("webview/setTitle")]
        public void SetTitle(string handle, string title)
        {
            System.Diagnostics.Debug.WriteLine(title, "Agent setTitle");
        }

        [AgentNotification("webview/setIconPath")]
        public void SetIconPath(string handle, string iconPathUri)
        {
            System.Diagnostics.Debug.WriteLine(iconPathUri, "Agent setIconPath");
        }

    }
}
