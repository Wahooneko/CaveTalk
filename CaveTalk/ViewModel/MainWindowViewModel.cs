﻿namespace CaveTalk.ViewModel {

	using System;
	using System.Collections.Generic;
	using System.Runtime.Remoting;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using CaveTalk.Utils;
	using FNF.Utility;

	public sealed class MainWindowViewModel : ViewModelBase {
		private CavetubeClient cavetubeClient;
		private BouyomiChanClient bouyomiClient;

		private String liveUrl;
		public String LiveUrl {
			get { return this.liveUrl; }
			set {
				this.liveUrl = value;
				base.OnPropertyChanged("LiveUrl");
			}
		}

		public Boolean bouyomiStatus;
		public Boolean BouyomiStatus {
			get { return this.bouyomiStatus;  }
			set {
				this.bouyomiStatus = value;
				base.OnPropertyChanged("BouyomiStatus");
			}
		}

		public IList<Message> MessageList { get; private set; }

		public ICommand ConnectCavetubeCommand { get; private set; }
		public ICommand SwitchBouyomiCommand { get; private set; }
		public ICommand AboutBoxCommand { get; private set; }

		public MainWindowViewModel() {
			this.MessageList = new SafeObservable<Message>();

			this.cavetubeClient = new CavetubeClient();
			this.cavetubeClient.OnMessage += (sender, message) => {
				this.MessageList.Insert(0, message);
				try {
					if (this.BouyomiStatus) {
						this.bouyomiClient.AddTalkTask(message.Comment);
					}
				} catch (RemotingException) {
					this.BouyomiStatus = false;
					MessageBox.Show("棒読みちゃんに接続できませんでした。");
				}
			};
			this.cavetubeClient.OnConnect += messages => {
				foreach(var message in messages) {
					this.MessageList.Insert(0, message);
				}
			};

			this.SwitchBouyomiCommand = new RelayCommand(param => {
				// CommandはClickなので、このイベントが走る時点で既にCheckedは切り替わっています。
				if (this.BouyomiStatus) {
					this.ConnectBouyomi();
				} else {
					this.DisconnectBouyomi();
				}
			});
			this.ConnectCavetubeCommand = new RelayCommand(ConnectCavetube);
			this.AboutBoxCommand = new RelayCommand(ShowVersion);

			this.ConnectBouyomi();
		}

		protected override void OnDispose() {
			if (this.cavetubeClient != null) {
				this.cavetubeClient.Dispose();
			}

			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
			}
		}

		private String ParseUrl(String url) {
			var pattern = @"(http://gae.cavelis.net/[a-z]+/)?([0-9A-Z]{32})";
			var match = Regex.Match(url, pattern);
			if (match.Success) {
				return match.Groups[2].Value;
			} else {
				return String.Empty;
			}
		}

		private void ConnectCavetube(object param) {
			var url = param as String;
			if (url == null) {
				return;
			}

			this.cavetubeClient.Close();
			this.MessageList.Clear();

			var roomId = this.ParseUrl(url);
			if (String.IsNullOrEmpty(roomId)) {
				return;
			}

			this.LiveUrl = roomId;
			this.cavetubeClient.Connect(roomId);
		}

		private void ConnectBouyomi() {
			this.DisconnectBouyomi();

			try {
				this.bouyomiClient = new BouyomiChanClient();
				var count = this.bouyomiClient.TalkTaskCount;
				this.BouyomiStatus = true;
			} catch (RemotingException) {
				MessageBox.Show("棒読みちゃんに接続できませんでした。");
			}
		}

		private void DisconnectBouyomi() {
			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
				this.BouyomiStatus = false;
			}
		}

		private void ShowVersion(object param) {
			new AboutBox().ShowDialog();
		}
	}
}