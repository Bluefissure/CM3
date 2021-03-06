﻿// Concept Matrix 3.
// Licensed under the MIT license.

namespace ConceptMatrix.GUI.Views
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using ConceptMatrix;
	using ConceptMatrix.GUI.Services;
	using ConceptMatrix.Injection.Offsets;
	using ConceptMatrix.WpfStyles;
	using FontAwesome.Sharp;

	/// <summary>
	/// Interaction logic for TargetSelectorView.xaml.
	/// </summary>
	public partial class TargetSelectorView : UserControl, IDrawer
	{
		public Actor Actor;

		private SelectionService selection;
		private IInjectionService injection;
		private bool lockSelection = false;

		public TargetSelectorView()
		{
			this.InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			this.selection = App.Services.Get<SelectionService>();
			this.injection = App.Services.Get<IInjectionService>();

			this.DataContext = this;

			this.GetEntities();
		}

		public event DrawerEvent Close;

		public ObservableCollection<PossibleSelection> Entities { get; set; } = new ObservableCollection<PossibleSelection>();
		public string InGameSelection { get; set; }
		public IconChar InGameIcon { get; set; }

		private void GetEntities()
		{
			try
			{
				this.lockSelection = true;
				Actor.Modes mode = this.selection.GetMode();
				ActorTableOffset actorTableOffset;
				BaseOffset targetOffset;

				// clear the entity list
				this.Entities.Clear();

				if (mode == Actor.Modes.GPose)
				{
					actorTableOffset = Offsets.Main.GposeActorTable;
					targetOffset = Offsets.Main.Gpose;
				}
				else if (mode == Actor.Modes.Overworld)
				{
					actorTableOffset = Offsets.Main.ActorTable;
					targetOffset = Offsets.Main.Target;
				}
				else
				{
					throw new Exception("Unknown selection mode: " + mode);
				}

				byte count = actorTableOffset.GetCount();
				HashSet<string> ids = new HashSet<string>();

				for (byte i = 0; i < count; i++)
				{
					ActorTypes type = actorTableOffset.GetActorValue(i, Offsets.Main.ActorType);
					string name = actorTableOffset.GetActorValue(i, Offsets.Main.Name);

					string id = SelectionService.GetActorId(mode, type, name);

					if (ids.Contains(id))
						continue;

					ids.Add(id);

					if (string.IsNullOrEmpty(name))
						name = "Unknown";

					PossibleSelection selection = new PossibleSelection(type, actorTableOffset.GetBaseOffset(i), id, name, mode);
					this.Entities.Add(selection);
				}

				if (this.selection.CurrentGameTarget != null)
				{
					this.InGameSelection = this.selection.CurrentGameTarget.Name;
					this.InGameIcon = this.selection.CurrentGameTarget.Type.GetIcon();
				}
				else
				{
					this.InGameSelection = null;
					this.InGameIcon = IconChar.None;
				}

				this.lockSelection = false;
			}
			catch (Exception ex)
			{
				Log.Write(ex);
			}
		}

		private void OnSelected(object sender, RoutedEventArgs e)
		{
			if (this.lockSelection)
				return;

			if (sender is RadioButton btn)
			{
				PossibleSelection selection = btn.DataContext as PossibleSelection;
				this.Actor = selection;

				this.Close?.Invoke();
			}
		}

		public class PossibleSelection : Actor
		{
			public PossibleSelection(ActorTypes type, IBaseMemoryOffset address, string actorId, string name, Modes mode)
				: base(type, address, actorId, name, mode)
			{
			}

			public bool IsSelected { get; set; }

			public IconChar Icon
			{
				get
				{
					return this.Type.GetIcon();
				}
			}
		}
	}
}
