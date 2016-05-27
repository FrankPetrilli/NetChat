using Android.App;
using Android.Widget;
using Android.OS;
using Quobject.SocketIoClientDotNet.Client;
using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Quobject.EngineIoClientDotNet.ComponentEmitter;
using Android.Telephony;

namespace NetChat
{
	[Activity (Label = "NetChat", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		private Socket socket;
		private ChatAdapter adapter;
		private List<ChatAdapter.ChatItem> chatItems = new List<ChatAdapter.ChatItem> ();
		private ListView chatWindow;
		private string username;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			SetContentView (Resource.Layout.Main);

			CreateSocket (); // Create socket to server
			chatWindow = FindViewById<ListView> (Resource.Id.chatWindow); // Where chat bubbles go
			adapter = new ChatAdapter (chatItems);
			chatWindow.Adapter = adapter;

			// Make the send button actually send the message using the socket.
			Button button = FindViewById<Button> (Resource.Id.sendButton);
			var telephonyManager = GetSystemService("phone") as TelephonyManager;
			username = telephonyManager.Line1Number + "@" + telephonyManager.NetworkOperatorName;
			System.Diagnostics.Debug.WriteLine (this.ActionBar.Subtitle);
			System.Diagnostics.Debug.WriteLine (this.ActionBar.Title);
			this.ActionBar.Subtitle = username; // += should work, but it doesn't.
			button.Click += delegate {
				socket.Emit ("message", JObject.FromObject (new { 
					message = FindViewById<TextView> (Resource.Id.entryText).Text, 
					room = "default", 
					username = username
				}));
				FindViewById<TextView>(Resource.Id.entryText).Text = "";
			};
		}

		// Delegate the ugly socket creation to another method.
		private void CreateSocket ()
		{
			IO.Options options = new IO.Options ();
			options.IgnoreServerCertificateValidation = true; // I'll make this HTTPS soon because security. :)
			options.Transports = Quobject.Collections.Immutable.ImmutableList<string>.Empty;
			options.Transports = options.Transports.Add ("websocket"); // xhr is ugly and has bugs... Force websocket.
			socket = IO.Socket ("http://dev.petril.li:3000", options);

			// Register events
			socket.On (Socket.EVENT_CONNECT, () => {
				System.Diagnostics.Debug.WriteLine ("connected");

				this.RunOnUiThread (() => {
					FindViewById<TextView> (Resource.Id.connected).Visibility = Android.Views.ViewStates.Gone; // Hide "connecting..." on connection.

				});

				socket.Emit ("subscribe", JObject.FromObject (new { room = "default" })); // Subscribe to the room.
			});

			/*socket.On ("message", data => {
				var obj = JObject.Parse (data);
				AddMessage (obj ["message"], obj ["username"]);
			});*/
			socket.On ("message", new ListenerImpl((data) => {
				System.Diagnostics.Debug.WriteLine(data);
				//var myObject = JObject.Parse (data);
				var myObject = (JObject)data;
				AddMessage (myObject ["message"].ToString(), myObject ["username"].ToString());
			}));
			socket.Open ();
		}

		private void AddMessage (string message, string username = null)
		{
			this.RunOnUiThread (() => {
				chatItems.Add (new ChatAdapter.ChatItem (username, message));

				adapter.NotifyDataSetChanged ();
				chatWindow.SetSelection (chatWindow.Count);
			});
		}
	}
}


