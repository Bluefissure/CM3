﻿// Concept Matrix 3.
// Licensed under the MIT license.

namespace ConceptMatrix.GUI.Services
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Threading.Tasks;
	using System.Windows.Controls;
	using ConceptMatrix;
	using ConceptMatrix.GUI.Windows;

	public class ViewService : IViewService
	{
		private List<Page> pendingPages = new List<Page>();
		private Dictionary<string, Page> pages = new Dictionary<string, Page>();
		private bool isStarted = false;

		public delegate void PageEvent(Page page);
		public delegate Task DrawerEvent(string title, UserControl drawer, DrawerDirection direction);

		public event PageEvent AddingPage;
		public event DrawerEvent ShowingDrawer;

		public IEnumerable<Page> Pages
		{
			get
			{
				return this.pages.Values;
			}
		}

		public Task Initialize()
		{
			return Task.CompletedTask;
		}

		public Task Shutdown()
		{
			return Task.CompletedTask;
		}

		public Task Start()
		{
			foreach (Page page in this.pendingPages)
			{
				page.Create();
			}

			this.pendingPages.Clear();

			this.isStarted = true;
			return Task.CompletedTask;
		}

		public void AddPage<T>(string name, string icon)
		{
			Page page = new Page();
			page.Icon = icon;
			page.Name = name;
			page.Type = typeof(T);

			if (this.pages.ContainsKey(name))
				throw new Exception($"Page already registered with name: {name}");

			if (!typeof(UserControl).IsAssignableFrom(page.Type))
				throw new Exception($"Page: {page.Type} does not extend from UserControl.");

			if (!this.isStarted)
			{
				this.pendingPages.Add(page);
			}
			else
			{
				page.Create();
			}

			this.pages.Add(name, page);
			this.AddingPage?.Invoke(page);
		}

		public Task ShowDrawer<T>(string title, DrawerDirection direction)
		{
			UserControl view = this.CreateView<T>();
			return this.ShowingDrawer?.Invoke(title, view, direction);
		}

		public Task ShowDrawer(object view, string title, DrawerDirection direction)
		{
			UserControl control = view as UserControl;

			if (control == null)
				throw new Exception("Invalid view");

			return this.ShowingDrawer?.Invoke(title, control, direction);
		}

		public Page GetPage(string path)
		{
			if (!this.pages.ContainsKey(path))
				throw new Exception($"View not found for path: {path}");

			return this.pages[path];
		}

		public Task<TResult> ShowDialog<TView, TResult>(string title)
			where TView : IDialog<TResult>
		{
			Dialog dlg = new Dialog();
			dlg.ContentArea.Content = this.CreateView<TView>();
			dlg.TitleText.Text = title;
			dlg.Owner = App.Current.MainWindow;

			IDialog<TResult> dialogInterface = dlg.ContentArea.Content as IDialog<TResult>;
			dialogInterface.Close += () => dlg.Close();

			dlg.ShowDialog();

			return Task.FromResult(dialogInterface.Result);
		}

		private UserControl CreateView<T>()
		{
			Type viewType = typeof(T);

			if (!typeof(UserControl).IsAssignableFrom(viewType))
				throw new Exception($"View: {viewType} does not extend from UserControl.");

			UserControl view;
			try
			{
				view = (UserControl)Activator.CreateInstance(viewType);
			}
			catch (TargetInvocationException ex)
			{
				Log.Write(new Exception($"Failed to create view: {viewType}", ex.InnerException));
				return null;
			}
			catch (Exception ex)
			{
				Log.Write(new Exception($"Failed to create view: {viewType}", ex));
				return null;
			}

			return view;
		}

		public class Page
		{
			public Type Type;
			public UserControl Instance;

			public string Name { get; set; }
			public string Icon { get; set; }

			public void Create()
			{
				try
				{
					App.Current.Dispatcher.Invoke(() =>
					{
						this.Instance = Activator.CreateInstance(this.Type) as UserControl;
					});
				}
				catch (TargetInvocationException ex)
				{
					Log.Write(new Exception($"Failed to create page: {this.Type}", ex.InnerException));
					return;
				}
				catch (Exception ex)
				{
					Log.Write(new Exception($"Failed to create page: {this.Type}", ex));
					return;
				}
			}
		}
	}
}
