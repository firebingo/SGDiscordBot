﻿using Microsoft.Extensions.DependencyInjection;
using SGMessageBot.Config;
using SGMessageBot.Helpers;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SGMessageBot.SteamBot
{
	public class SteamMain
	{
		private SteamClient steamClient;
		private CallbackManager callbackManager;
		private SteamActions steamHandler;
		private SteamUser steamUser;
		private SteamFriends steamFriends;
		private IServiceProvider serviceProvider = null;

		private bool isRunning;
		public bool isConnected;
		public string steamNonce = string.Empty;
		public EAccountFlags AccountFlags;
		public ulong SteamID;

		public Task RunBot()
		{
			steamClient = new SteamClient();
			callbackManager = new CallbackManager(steamClient);
			steamHandler = new SteamActions();

			steamUser = steamClient.GetHandler<SteamUser>();
			steamFriends = steamClient.GetHandler<SteamFriends>();

			callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
			callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

			callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
			callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
			callbackManager.Subscribe<SteamFriends.ChatMsgCallback>(OnMessageReceived);

			//steamguard
			callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);

			isRunning = true;
			Console.WriteLine("Connecting to Steam...");
			steamClient.Connect();

			serviceProvider = ConfigureServices();
			steamHandler.InstallServiceMap(serviceProvider);
			Task.Run(() => DoCallbackLoop());
			return Task.CompletedTask;
		}

		private Task DoCallbackLoop()
		{
			while (isRunning)
			{
				// in order for the callbacks to get routed, they need to be handled by the manager
				callbackManager.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(500));
			}
			return Task.CompletedTask;
		}

		private IServiceProvider ConfigureServices()
		{
			//setup and add command service.
			var services = new ServiceCollection()
				.AddSingleton(steamClient)
				.AddSingleton(SGMessageBot.BotConfig)
				.AddSingleton(callbackManager)
				.AddSingleton(steamUser)
				.AddSingleton(steamFriends);
			var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
			return provider;
		}

		private void OnConnected(SteamClient.ConnectedCallback callback)
		{
			Console.WriteLine($"Connected to Steam! Logging in '{SGMessageBot.BotConfig.BotInfo.SteamConfig.Username}'...");

			byte[] sentryHash = null;
			if (File.Exists(SGMessageBot.BotConfig.BotInfo.SteamConfig.SentryFileLocation))
			{
				// if we have a saved sentry file, read and sha-1 hash it
				byte[] sentryFile = File.ReadAllBytes(SGMessageBot.BotConfig.BotInfo.SteamConfig.SentryFileLocation);
				sentryHash = CryptoHelper.SHAHash(sentryFile);
			}

			steamUser.LogOn(new SteamUser.LogOnDetails
			{
				Username = SGMessageBot.BotConfig.BotInfo.SteamConfig.Username,
				Password = SGMessageBot.BotConfig.BotInfo.SteamConfig.Password,
				AuthCode = sentryHash == null ? SGMessageBot.BotConfig.BotInfo.SteamConfig.AuthCode : null,
				TwoFactorCode = sentryHash == null ? SGMessageBot.BotConfig.BotInfo.SteamConfig.TwoFactorCode : null,
				SentryFileHash = sentryHash ?? null
			});
			isConnected = true;
		}

		private void OnDisconnected(SteamClient.DisconnectedCallback callback)
		{
			isConnected = false;
			Console.WriteLine("Disconnected from Steam, reconnecting in 10...");

			Thread.Sleep(TimeSpan.FromSeconds(10));

			steamClient.Connect();
		}

		private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
		{
			bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
			bool is2fa = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

			if (isSteamGuard || is2fa)
			{
				Console.WriteLine("This account is SteamGuard protected!");

				if (is2fa)
				{
					Console.Write("Please enter your 2 factor code into the config file and restart");
					isRunning = false;
				}
				else
				{
					Console.Write($"Please enter the auth code into the config file sent to the email at {callback.EmailDomain} and restart");
					isRunning = false;
				}

				return;
			}

			if (callback.Result != EResult.OK)
			{
				Console.WriteLine($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
				ErrorLog.WriteLog($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");

				isRunning = false;
				return;
			}

			Console.WriteLine("Successfully logged on!");

			AccountFlags = callback.AccountFlags;
			SteamID = callback.ClientSteamID;

			steamHandler.GetGroups();
			steamHandler.ConnectToGroupChats();
		}

		private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
		{
			Console.WriteLine("Logged off of Steam: {0}", callback.Result);
		}

		private void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
		{
			Console.WriteLine("Updating Steam sentryfile...");
			var fileName = $"Data/{callback.FileName}";

			int fileSize;
			byte[] sentryHash;
			using (var fs = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				fs.Seek(callback.Offset, SeekOrigin.Begin);
				fs.Write(callback.Data, 0, callback.BytesToWrite);
				fileSize = (int)fs.Length;

				fs.Seek(0, SeekOrigin.Begin);
				using var sha = SHA1.Create();
				sentryHash = sha.ComputeHash(fs);
			}

			// inform the steam servers that we're accepting this sentry file
			steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
			{
				JobID = callback.JobID,

				FileName = callback.FileName,

				BytesWritten = callback.BytesToWrite,
				FileSize = fileSize,
				Offset = callback.Offset,

				Result = EResult.OK,
				LastError = 0,

				OneTimePassword = callback.OneTimePassword,

				SentryFileHash = sentryHash,
			});

			SGMessageBot.BotConfig.BotInfo.SteamConfig.SentryFileLocation = fileName;
			SGMessageBot.BotConfig.SaveCredConfig();
		}

		private void OnMessageReceived(SteamFriends.ChatMsgCallback callback)
		{
			Task.Run(() => steamHandler.HandleMessageReceived(callback));
		}
	}
}
