﻿namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;
	using System.Configuration;
	using NLog;
	using System.Threading.Tasks;
	using System.Threading;
	using CaveTube.CaveTalk.CaveTubeClient;

	public sealed class LoginBoxViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private Cursor cursor;
		public Cursor Cursor {
			get { return this.cursor; }
			set {
				this.cursor = value;
				base.OnPropertyChanged("Cursor");
			}
		}

		private String userId;
		public String UserId {
			get { return this.userId; }
			set {
				this.userId = value;
				base.OnPropertyChanged("UserId");
			}
		}

		private String password;
		public String Password {
			get { return this.password; }
			set {
				this.password = value;
				base.OnPropertyChanged("Password");
			}
		}

		private String errorMessage;
		public String ErrorMessage {
			get { return this.errorMessage; }
			set {
				this.errorMessage = value;

				Task.Factory.StartNew(() => {
					Thread.Sleep(2000);
					this.errorMessage = String.Empty;
					base.OnPropertyChanged("ErrorMessage");
				});

				base.OnPropertyChanged("ErrorMessage");
			}
		}

		public ICommand LoginCommand { get; private set; }

		public event Action OnClose;

		private CavetubeClient client;

		public LoginBoxViewModel(CavetubeClient client) {
			this.LoginCommand = new RelayCommand(Login);
			this.client = client;
		}

		private void Login(Object data) {
			this.Cursor = Cursors.Wait;
			try {
				var devKey = ConfigurationManager.AppSettings["dev_key"];
				if (String.IsNullOrWhiteSpace(devKey)) {
					throw new ConfigurationErrorsException("[dev_key]が設定されていません。");
				}

				try {
					var apiKey = this.client.Login(this.UserId, this.Password, devKey);
					if (String.IsNullOrWhiteSpace(apiKey)) {
						this.ErrorMessage = "ログインに失敗しました。";
						return;
					}
					CaveTalk.Properties.Settings.Default.ApiKey = apiKey;
					CaveTalk.Properties.Settings.Default.UserId = this.UserId;
					CaveTalk.Properties.Settings.Default.Password = this.Password;
					CaveTalk.Properties.Settings.Default.Save();

				} catch (ArgumentException e) {
					var message = "ログインに失敗しました。";
					this.ErrorMessage = message;
					logger.Error(message, e);
					return;
				}
			} finally {
				this.Cursor = null;
			}

			if (this.OnClose != null) {
				this.OnClose();
			}
		}
	}
}