using System;
using Eto.Forms;
using System.Linq;
using Eto.Drawing;
using System.Diagnostics;

#if IOS
using MonoTouch.UIKit;
using NSView = MonoTouch.UIKit.UIView;
using IMacView = Eto.Platform.iOS.Forms.IiosView;
namespace Eto.Platform.iOS.Forms

#elif OSX
using MonoMac.AppKit;
namespace Eto.Platform.Mac
#endif
{
	public class TableLayoutHandler : 
#if IOS
		iosLayout<NSView, TableLayout>, ITableLayout
#elif OSX
		MacLayout<NSView, TableLayout>, ITableLayout
#endif
	{
		Control[,] views;
		bool[] xscaling;
		bool[] yscaling;
		Size spacing;
		Padding padding;
		bool loaded;
		
		public override void Update ()
		{
			Layout ();
		}
		
		public Eto.Drawing.Size Spacing {
			get { return spacing; }
			set {
				spacing = value;
				if (Widget.Loaded)
					Layout ();
			}
		}
		
		public Padding Padding {
			get { return padding; }
			set {
				padding = value;
				if (Widget.Loaded)
					Layout ();
			}
		}
		
		public override NSView Control {
			get {
				return (NSView)Widget.Container.ContainerObject;
			}
			protected set {
				base.Control = value;
			}
		}
		
		/*
		public override object LayoutObject
		{
			get	{ return Control; }
		}*/

		public TableLayoutHandler ()
		{
			/*Control = new NSView();
			Control.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
			Control.PostsFrameChangedNotifications = true;
			this.AddObserver(NSView.NSViewFrameDidChangeNotification, delegate { 
				Layout();
			});*/
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			this.Spacing = TableLayout.DefaultSpacing;
			this.Padding = TableLayout.DefaultPadding;
		}
		
		public override void SizeToFit ()
		{
			if (views == null)
				return;
			var heights = new float[views.GetLength (0)];
			var widths = new float[views.GetLength (1)];
			var controlFrame = Control.Frame;
			float totalxpadding = Padding.Horizontal + Spacing.Width * (widths.Length - 1);
			float totalypadding = Padding.Vertical + Spacing.Height * (heights.Length - 1);
			var requiredx = totalxpadding;
			var requiredy = totalypadding;
			
			for (int y=0; y<heights.Length; y++) {
				heights [y] = 0;
			}
			for (int x=0; x<widths.Length; x++) {
				widths [x] = 0;
			}
			
			for (int y=0; y<heights.Length; y++)
				for (int x=0; x<widths.Length; x++) {
					var view = views [y, x];
					if (view != null && view.Visible) {
						AutoSize (view);
						//Console.WriteLine ("CALC: x:{2} y:{3} view: {0} size: {1}", view, view.Size, x, y);
						var macview = view.Handler as IMacView;
						var size = view.Size;
						if (macview != null) {
							size = macview.PreferredSize ?? size;
							var minsize = macview.MinimumSize;
							if (minsize != null) {
								size.Width = Math.Max (size.Width, minsize.Value.Width);
								size.Height = Math.Max (size.Height, minsize.Value.Height);
							}
						}
						if (size.Width > widths [x]) { 
							requiredx += size.Width - widths [x];
							widths [x] = size.Width;
						}
						if (size.Height > heights [y]) { 
							requiredy += size.Height - heights [y];
							heights [y] = size.Height;
						}
					}
				}
			controlFrame.Width = requiredx;
			controlFrame.Height = requiredy;
			//Console.WriteLine("TableLayout container size: {0}", controlFrame.Size);
			SetContainerSize (controlFrame.Size);
		}
		
		void Layout ()
		{
			if (views == null)
				return;
			var heights = new float[views.GetLength (0)];
			var widths = new float[views.GetLength (1)];
			var controlFrame = Control.Frame;
			float totalxpadding = Padding.Horizontal + Spacing.Width * (widths.Length - 1);
			float totalypadding = Padding.Vertical + Spacing.Height * (heights.Length - 1);
			var totalx = controlFrame.Width - totalxpadding;
			var totaly = controlFrame.Height - totalypadding;
			var requiredx = totalxpadding;
			var requiredy = totalypadding;
			var numx = 0;
			var numy = 0;
			
			for (int y=0; y<heights.Length; y++) {
				heights [y] = 0;
				if (yscaling [y])
					numy++;
			}
			for (int x=0; x<widths.Length; x++) {
				widths [x] = 0;
				if (xscaling [x])
					numx++;
			}
			
			for (int y=0; y<heights.Length; y++)
				for (int x=0; x<widths.Length; x++) {
					var view = views [y, x];
					if (view != null && view.Visible) {
						AutoSize (view);
						var macview = view.Handler as IMacView;
						var size = view.Size;
						if (macview != null) {
							size = macview.PreferredSize ?? view.Size;
							var minsize = macview.MinimumSize;
							if (minsize != null) {
								size.Width = Math.Max (size.Width, minsize.Value.Width);
								size.Height = Math.Max (size.Height, minsize.Value.Height);
							}
						}
						//Console.WriteLine ("x:{2} y:{3} view: {0} size: {1} totalx:{4} totaly:{5}", view, view.Size, x, y, totalx, totaly);
						if (!xscaling [x] && widths [x] < size.Width) { 
						
							widths [x] = size.Width;
							requiredx += size.Width - widths [x];
						}
						if (!yscaling [y] && heights [y] < size.Height) { 
						
							heights [y] = size.Height;
							requiredy += size.Height - heights [y];
						}
					}
				}
			if (controlFrame.Width < requiredx) {
				totalx = requiredx - totalxpadding;
				controlFrame.Width = requiredx;
			}
			if (controlFrame.Height < requiredy) {
				//Console.WriteLine("restricting y from {0} to {1}", controlFrame.Height, requiredy);
				totaly = requiredy - totalypadding;
				controlFrame.Height = requiredy;
			}
			
			if (numy > 0) {
				for (int y=0; y<heights.Length; y++)
					if (!yscaling [y])
						totaly -= heights [y];
			} else {
				if (heights.Length > 1) {
					for (int y=0; y<heights.Length-1; y++)
						totaly -= heights [y];
				}
				heights [heights.Length - 1] = totaly;
			}
			if (numx > 0) {
				for (int x=0; x<widths.Length; x++)
					if (!xscaling [x])
						totalx -= widths [x];
			} else { 
				if (widths.Length > 1) {
					for (int x=0; x<widths.Length-1; x++)
						totalx -= widths [x];
				}
				widths [widths.Length - 1] = totalx;
			}
			
			var chunkx = (numx > 0) ? (float)Math.Truncate (Math.Max (totalx, 0) / numx) : totalx;
			var chunky = (numy > 0) ? (float)Math.Truncate (Math.Max (totaly, 0) / numy) : totaly;
			
#if OSX			
			bool flipped = Control.IsFlipped;
#elif IOS
			bool flipped = Control.Layer.GeometryFlipped;
#endif
			float starty = Padding.Top;
			for (int x=0; x<widths.Length; x++) {
				if (xscaling [x]) {
					widths [x] = Math.Min (chunkx, totalx);
					totalx -= chunkx;
				}
			}
			
			for (int y=0; y<heights.Length; y++) {
				if (yscaling [y]) {
					heights [y] = Math.Min (chunky, totaly);
					totaly -= chunky;
				}
				float startx = Padding.Left;
				for (int x=0; x<widths.Length; x++) {
					var view = views [y, x];
					if (view != null && view.Visible) {
						var nsview = view.ControlObject as NSView;
						var frame = nsview.Frame;
						frame.Width = widths [x];
						frame.Height = heights [y];
						frame.X = startx;
						frame.Y = flipped ? starty : Control.Frame.Height - starty - frame.Height;
						if (frame != nsview.Frame)
							nsview.Frame = frame;
						//Console.WriteLine ("*** x:{2} y:{3} view: {0} size: {1} totalx:{4} totaly:{5}", view, view.Size, x, y, totalx, totaly);
					}
					startx += widths [x] + Spacing.Width;
				}
				starty += heights [y] + Spacing.Height;
			}
		}

		public void Add (Control child, int x, int y)
		{
			var current = views [y, x];
			if (current != null) {
				var currentView = (NSView)current.ControlObject;
				if (currentView != null)
					currentView.RemoveFromSuperview ();	
			}
			var view = (NSView)child.ControlObject;
			views [y, x] = child;
			if (loaded)
				Layout ();
			Control.AddSubview (view);
		}

		public void Move (Control child, int x, int y)
		{
			var current = views [y, x];
			if (current != null) {
				var currentView = (NSView)current.ControlObject;
				if (currentView != null)
					currentView.RemoveFromSuperview ();	
			}

			views [y, x] = child;
			if (loaded)
				Layout ();
		}
		
		public void Remove (Control child)
		{
			var view = (NSView)child.ControlObject;
			view.RemoveFromSuperview ();
			for (int y=0; y<views.GetLength(0); y++)
				for (int x=0; x<views.GetLength(1); x++) {
					if (views [y, x] == child)
						views [y, x] = null;
				}
			if (loaded)
				Layout ();
		}
		
		public override void OnLoadComplete ()
		{
			base.OnLoadComplete ();
			Layout ();
			
#if OSX
			Control.PostsFrameChangedNotifications = true;
			this.AddObserver (NSView.NSViewFrameDidChangeNotification, delegate(ObserverActionArgs e) { 
				var handler = e.Widget.Handler as TableLayoutHandler;
				handler.Layout ();
			});
#elif IOS
			Widget.Container.SizeChanged += delegate(object sender, EventArgs e) {
				Console.WriteLine ("Layout!");
				Layout();
			};
#endif
			loaded = true;
		}
		
		public void CreateControl (int cols, int rows)
		{
			views = new Control[rows, cols];
			xscaling = new bool[cols];
			yscaling = new bool[rows];
		}

		public void SetColumnScale (int column, bool scale)
		{
			xscaling [column] = scale;
		}
		
		public void SetRowScale (int row, bool scale)
		{
			yscaling [row] = scale;
		}
	}
}
