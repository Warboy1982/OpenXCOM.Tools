using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using MapView.Forms.MainWindow;

using XCom;
using XCom.Interfaces.Base;


namespace MapView
{
	public class MapView // NOTE: namespace conflict w/ .NET itself
		:
		Panel
	{
		private IMap_Base _baseMap;

		private Point _origin = new Point(100, 0);

		private CursorSprite _cursor;

//		private Size _viewsize;

		private const int HalfWidth  = 16;
		private const int HalfHeight =  8;

		private Point _dragStart;
		private Point _dragEnd;

		private Pen _penDash;

		private bool _selectGrayscale = true;

		private GraphicsPath _gridUnder;
		private Brush _brushTrans;
		private Color _colorGrid;

		private bool _useGrid = true;
		private bool _drawSelectionBox;

		private MapTileBase[,] _copied;

		public bool DrawSelectionBox
		{
			get { return _drawSelectionBox; }
			set
			{
				_drawSelectionBox = value;
				Refresh();
			}
		}

		public event EventHandler DragChanged;


		public MapView()
		{
			_baseMap = null;

			_dragStart =
			_dragEnd   = new Point(-1, -1);

			SetStyle(
					ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint,
					true);

			_colorGrid  = Color.FromArgb(175, 69, 100, 129);
			_brushTrans = new SolidBrush(_colorGrid);
			_penDash    = new Pen(Brushes.Black, 1);
		}


		public void Paste()
		{
			if (_baseMap != null && _copied != null)
			{
				var dragStart = DragStart;
				for (int
						r = dragStart.Y;
						r != _baseMap.MapSize.Rows && (r - dragStart.Y) < _copied.GetLength(0);
						++r)
					for (int
							c = dragStart.X;
							c != _baseMap.MapSize.Cols && (c - dragStart.X) < _copied.GetLength(1);
							++c)
					{
						var tile = _baseMap[r, c] as XCMapTile;
						if (tile != null)
						{
							var copyTile = _copied[r - dragStart.Y, c - dragStart.X] as XCMapTile;
							if (copyTile != null)
							{
								tile.Ground  = copyTile.Ground;
								tile.Content = copyTile.Content;
								tile.West    = copyTile.West;
								tile.North   = copyTile.North;
							}
						}
					}

				_baseMap.MapChanged = true;
				Refresh();
			}
		}

		public bool SelectGrayscale
		{
			get { return _selectGrayscale; }
//			set
//			{
//				_selectGrayscale = value;
//				Refresh();
//			}
		}

		public void ClearSelection()
		{
			if (_baseMap != null)
			{
				var start = GetDragStart();
				var end = GetDragEnd();
	
				for (int c = start.X; c <= end.X; c++)
					for (int r = start.Y; r <= end.Y; r++)
						_baseMap[r, c] = XCMapTile.BlankTile;

				_baseMap.MapChanged = true;
				Refresh();
			}
		}

		public void Copy()
		{
			if (_baseMap != null)
			{
				var start = GetDragStart();
				var end   = GetDragEnd();

				_copied = new MapTileBase[end.Y - start.Y + 1, end.X - start.X + 1];

				for (int c = start.X; c <= end.X; ++c)
					for (int r = start.Y; r <= end.Y; ++r)
						_copied[r - start.Y, c - start.X] = _baseMap[r, c];
			}
		}

		public Color GridColor
		{
			get { return _colorGrid; }
			set
			{
				_colorGrid = value;
				_brushTrans = new SolidBrush(value);
				Refresh();
			}
		}

		public Color GridLineColor
		{
			get { return _penDash.Color; }
			set
			{
				_penDash.Color = value;
				Refresh();
			}
		}

		public int GridLineWidth
		{
			get { return (int)_penDash.Width; }
			set
			{
				_penDash.Width = value;
				Refresh();
			}
		}

		public bool UseGrid
		{
			get { return _useGrid; }
			set
			{
				_useGrid = value;
				Refresh();
			}
		}

		internal CursorSprite CursorSprite
		{
//			get { return cursor; }
			set
			{
				_cursor = value;
				Refresh();
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (_baseMap != null)
			{
				var dragStart = ConvertCoordsDiamond(
												e.X, e.Y,
												_baseMap.CurrentHeight);
				var dragEnd = ConvertCoordsDiamond(
												e.X, e.Y,
												_baseMap.CurrentHeight);
				SetDrag(dragStart, dragEnd);

				_baseMap.SelectedTile = new MapLocation(
												DragEnd.Y, DragEnd.X,
												_baseMap.CurrentHeight);

				Focus();
				Refresh();
			}
		}

		/// <summary>
		/// Scrolls the z-axis for MapView.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			if		(e.Delta < 0) _baseMap.Up();
			else if	(e.Delta > 0) _baseMap.Down();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			Refresh();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (_baseMap != null)
			{
				var pt = ConvertCoordsDiamond(
											e.X, e.Y,
											_baseMap.CurrentHeight);

				if (pt.X != DragEnd.X || pt.Y != DragEnd.Y)
				{
					if (e.Button != MouseButtons.None)
						SetDrag(DragStart, pt);

					Refresh(); // mouseover refresh for MapView.
				}
			}
		}

		public Point DragStart
		{
			get { return _dragStart; }
			private set
			{
				_dragStart = value;
				if		(_dragStart.Y < 0) _dragStart.Y = 0;
				else if	(_dragStart.Y >= _baseMap.MapSize.Rows) _dragStart.Y = _baseMap.MapSize.Rows - 1;

				if		(_dragStart.X < 0) _dragStart.X = 0;
				else if	(_dragStart.X >= _baseMap.MapSize.Cols) _dragStart.X = _baseMap.MapSize.Cols - 1;
			}
		}

		public Point DragEnd
		{
			get { return _dragEnd; }
			private set
			{
				_dragEnd = value;
				if		(_dragEnd.Y < 0) _dragEnd.Y = 0;
				else if	(_dragEnd.Y >= _baseMap.MapSize.Rows) _dragEnd.Y = _baseMap.MapSize.Rows - 1;

				if		(_dragEnd.X < 0) _dragEnd.X = 0;
				else if	(_dragEnd.X >= _baseMap.MapSize.Cols) _dragEnd.X = _baseMap.MapSize.Cols - 1;
			}
		}

		public void SetDrag(Point dragStart, Point dragEnd)
		{
			if (DragEnd != dragEnd || DragStart != dragStart)
			{
				DragStart = dragStart;
				DragEnd = dragEnd;
	
				if (DragChanged != null)
					DragChanged(this, EventArgs.Empty);

				Refresh();
			}
		}

		public IMap_Base Map
		{
			get { return _baseMap; }
			set
			{
				if (_baseMap != null)
				{
					_baseMap.HeightChanged -= MapHeight;
					_baseMap.SelectedTileChanged -= TileChange;
				}

				if ((_baseMap = value) != null)
				{
					_baseMap.HeightChanged += MapHeight;
					_baseMap.SelectedTileChanged += TileChange;
					SetupMapSize();

					DragStart = DragStart; // Calculate drag
					DragEnd   = DragEnd;
				}
			}
		}

		public void SetupMapSize()
		{
			if (_baseMap != null)
			{
				var size = GetMapSize(Globals.PckImageScale);
				Width  = size.Width;
				Height = size.Height;
			}
		}

		public Size GetMapSize(double pckImageScale)
		{
			if (_baseMap != null)
			{
				var halfWidth  = (int)(HalfWidth  * pckImageScale);
				var halfHeight = (int)(HalfHeight * pckImageScale);

				_origin = new Point((_baseMap.MapSize.Rows - 1) * halfWidth, 0);

				var width = (_baseMap.MapSize.Rows + _baseMap.MapSize.Cols) * halfWidth;
				var height = _baseMap.MapSize.Height * (halfHeight * 3) +
							(_baseMap.MapSize.Rows + _baseMap.MapSize.Cols) * halfHeight;

				return new Size(width, height);
			}
			return Size.Empty;
		}

/*		public Size Viewable
		{
//			get { return _viewsize; }
			set { _viewsize = value; }
		} */

		private void TileChange(IMap_Base baseMap, SelectedTileChangedEventArgs e) // MapLocation newCoords)
		{
			MapLocation newCoords = e.MapPosition;
			var dragStart = new Point(
									newCoords.Col,
									newCoords.Row);
			SetDrag(dragStart, DragEnd);

			XCMainWindow.Instance.StatusBarPrintPosition(newCoords.Col, newCoords.Row);
		}

		private void MapHeight(IMap_Base baseMap, HeightChangedEventArgs e)
		{
			Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (_baseMap != null)
			{
				var g = e.Graphics;

				var dragMin = new Point(
									Math.Min(DragStart.X, DragEnd.X),
									Math.Min(DragStart.Y, DragEnd.Y));
				var dragMax = new Point(
									Math.Max(DragStart.X, DragEnd.X),
									Math.Max(DragStart.Y, DragEnd.Y));

				var dragRect = new Rectangle(dragMin, new Size(Point.Subtract(dragMax, new Size(dragMin))));
				dragRect.Width  += 1;
				dragRect.Height += 1;

				var insideDragRect = dragRect;
				insideDragRect.X += 1;
				insideDragRect.Y += 1;
				insideDragRect.Width  -= 2;
				insideDragRect.Height -= 2;

				var halfWidth  = (int)(HalfWidth  * Globals.PckImageScale);
				var halfHeight = (int)(HalfHeight * Globals.PckImageScale);

				for (var h = _baseMap.MapSize.Height - 1; h > -1; --h)
				{
					if (_baseMap.CurrentHeight <= h)
					{
						DrawGrid(h, g);

						var startY = _origin.Y + (halfHeight * 3 * h);
						var startX = _origin.X;

						for (var row = 0; row != _baseMap.MapSize.Rows; ++row)
						{
							var x = startX;
							var y = startY;
							for (var col = 0; col != _baseMap.MapSize.Cols; ++col)
							{
								var isClickedLocation = IsDragEndOrStart(row, col);
								var tileRect = new Rectangle(col, row, 1, 1);

								if (isClickedLocation)
									DrawCursor(g, x, y, h);

								if (_baseMap.CurrentHeight == h || _baseMap[row, col, h].DrawAbove)
								{
									var tile = (XCMapTile) _baseMap[row, col, h];
									if (_selectGrayscale
										&& (isClickedLocation || dragRect.IntersectsWith(tileRect)))
									{
										DrawTileGray(g, tile, x, y);
									}
									else
										DrawTile(g, tile, x, y);
								}

								if (isClickedLocation && _cursor != null)
									_cursor.DrawLow(
												g,
												x, y,
												MapViewPanel.Current,
												false,
												_baseMap.CurrentHeight == h);

								x += halfWidth;
								y += halfHeight;
							}

							startY += halfHeight;
							startX -= halfWidth;
						}
					}
				}

				if (_drawSelectionBox)
					DrawSelection(g, _baseMap.CurrentHeight, dragRect);
			}
		}

		private void DrawCursor(Graphics g, int x, int y, int h)
		{
			if (_cursor != null)
				_cursor.DrawHigh(
							g,
							x, y,
							false,
							_baseMap.CurrentHeight == h);
		}

		private bool IsDragEndOrStart(int row, int col)
		{
			return (row == DragEnd.Y   && col == DragEnd.X)
				|| (row == DragStart.Y && col == DragStart.X);
		}

		private void DrawGrid(int h, Graphics g)
		{
			if (_useGrid && _baseMap.CurrentHeight == h)
			{
				var hWidth  = (int)(HalfWidth  * Globals.PckImageScale);
				var hHeight = (int)(HalfHeight * Globals.PckImageScale);

				var x = hWidth + _origin.X;
				var y = ((_baseMap.CurrentHeight + 1) * (hHeight * 3)) + _origin.Y;

				var xMax = _baseMap.MapSize.Rows * hWidth;
				var yMax = _baseMap.MapSize.Rows * hHeight;

				_gridUnder = new GraphicsPath();

				var pt0 = new Point(x, y);
				var pt1 = new Point(
								x + _baseMap.MapSize.Cols * hWidth,
								y + _baseMap.MapSize.Cols * hHeight);
				var pt2 = new Point(
								x + (_baseMap.MapSize.Cols - _baseMap.MapSize.Rows) * hWidth,
								y + (_baseMap.MapSize.Rows + _baseMap.MapSize.Cols) * hHeight);
				var pt3 = new Point(x - xMax, yMax + y);

				_gridUnder.AddLine(pt0, pt1);
				_gridUnder.AddLine(pt1, pt2);
				_gridUnder.AddLine(pt2, pt3);
				_gridUnder.CloseFigure();

				g.FillPath(_brushTrans, _gridUnder);

				for (var i = 0; i <= _baseMap.MapSize.Rows; ++i)
					g.DrawLine(
							_penDash,
							x - hWidth  * i,
							y + hHeight * i,
							x + (_baseMap.MapSize.Cols - i) * hWidth,
							y + (_baseMap.MapSize.Cols + i) * hHeight);

				for (int i = 0; i <= _baseMap.MapSize.Cols; ++i)
					g.DrawLine(
							_penDash,
							x + hWidth  * i,
							y + hHeight * i,
							hWidth  * i - xMax + x,
							hHeight * i + yMax + y);
			}
		}

		private static void DrawTile(Graphics g, XCMapTile tile, int x, int y)
		{
			var topView = MainWindowsManager.TopView.Control;
			if (tile.Ground != null && topView.GroundVisible)
				DrawTile(g, x, y, tile.Ground);

			if (tile.North != null && topView.NorthVisible)
				DrawTile(g, x, y, tile.North);

			if (tile.West != null && topView.WestVisible)
				DrawTile(g, x, y, tile.West);

			if (tile.Content != null && topView.ContentVisible)
				DrawTile(g, x, y, tile.Content);
		}

		private static void DrawTileGray(Graphics g, XCMapTile tile, int x, int y)
		{
			var topView = MainWindowsManager.TopView.Control;
			if (tile.Ground != null && topView.GroundVisible)
				DrawTileGray(g, x, y, tile.Ground);

			if (tile.North != null && topView.NorthVisible)
				DrawTileGray(g, x, y, tile.North);

			if (tile.West != null && topView.WestVisible)
				DrawTileGray(g, x, y, tile.West);

			if (tile.Content != null && topView.ContentVisible)
				DrawTileGray(g, x, y, tile.Content);
		}

		private static void DrawTile(Graphics g, int x, int y, TileBase tile)
		{
			var image = tile[MapViewPanel.Current].Image;
			DrawTile(g, x, y, tile, image);
		}

		private static void DrawTileGray(Graphics g, int x, int y, TileBase tile)
		{
			var image = tile[MapViewPanel.Current].Gray;
			DrawTile(g, x, y, tile, image);
		}

		private static void DrawTile(Graphics g, int x, int y, TileBase tile, Image image)
		{
			g.DrawImage(
					image,
					x,
					(int)(y - tile.Info.TileOffset * Globals.PckImageScale),
					(int)(image.Width  * Globals.PckImageScale),
					(int)(image.Height * Globals.PckImageScale));
		}

		private void DrawSelection(Graphics g, int h, Rectangle dragRect)
		{
			var hWidth = (int)(HalfWidth * Globals.PckImageScale);

			var top    = ConvertCoordsRect(new Point(dragRect.X,     dragRect.Y), h + 1);
			var right  = ConvertCoordsRect(new Point(dragRect.Right, dragRect.Y), h + 1);
			var bottom = ConvertCoordsRect(new Point(dragRect.Right, dragRect.Bottom), h + 1);
			var left   = ConvertCoordsRect(new Point(dragRect.Left,  dragRect.Bottom), h + 1);

			top.X    += hWidth;
			right.X  += hWidth;
			bottom.X += hWidth;
			left.X   += hWidth;

			var pen = new Pen(Color.FromArgb(70, Color.Red));
			pen.Width = 3;

			g.DrawLine(pen, top, right);
			g.DrawLine(pen, right, bottom);
			g.DrawLine(pen, bottom, left);
			g.DrawLine(pen, left, top);
		}

		/// <summary>
		/// Converts a point from screen coordinates to tile coordinates.
		/// </summary>
		/// <param name="ptX"></param>
		/// <param name="ptY"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		private Point ConvertCoordsDiamond(int ptX, int ptY, int level)
		{
			// 16 is half the width of the diamond
			// 24 is the distance from the top of the diamond to the very top of the image

			var halfWidth  = HalfWidth  * Globals.PckImageScale;
			var halfHeight = HalfHeight * Globals.PckImageScale;

			var x = (ptX - _origin.X - halfWidth);
			var y = (ptY - _origin.Y - halfHeight * 3 * (level + 1));

			var x1 = (x / (halfWidth  * 2)) +
					 (y / (halfHeight * 2));
			var x2 = -(x - y * 2) / (halfWidth * 2);

			return new Point(
						(int)Math.Floor(x1),
						(int)Math.Floor(x2));

//			LogFile.Instance.WriteLine(
//									"\n" +
//									"ptX= " + ptX + "\n" +
//									"ptY= " + ptY + "\n" +
//									"halfWidth= " + halfWidth + "\n" +
//									"halfHeight= " + halfHeight + "\n" +
//									"x= " + x + "\n" +
//									"y= " + y + "\n" +
//									"x1= " + x1 + "\n" +
//									"x2= " + x2);

/*			// try this for a while ... ->
			if (x < 0) // left side of Map diamond
			{
//				LogFile.Instance.WriteLine("left side");
				return new Point(
							(int)Math.Floor(x1),
							(int)Math.Round(x2));
			}

//			LogFile.Instance.WriteLine("right side");
			return new Point(
						(int)Math.Round(x1),
						(int)Math.Floor(x2)); */
		}

		private Point ConvertCoordsRect(Point pt, int h)
		{
			var hWidth  = (int)(HalfWidth  * Globals.PckImageScale);
			var hHeight = (int)(HalfHeight * Globals.PckImageScale);
			int x = pt.X;
			int y = pt.Y;
			var heightAdjust = (hHeight * 3 * h);
			return new Point(
						 _origin.X + (hWidth  * (x - y)),
						(_origin.Y + (hHeight * (x + y))) + heightAdjust);
		}

		private Point GetDragStart()
		{
			var start = new Point();
			start.X = Math.Max(Math.Min(DragStart.X, DragEnd.X), 0);
			start.Y = Math.Max(Math.Min(DragStart.Y, DragEnd.Y), 0);
			return start;
		}

		private Point GetDragEnd()
		{
			var end = new Point();
			end.X = Math.Max(DragStart.X, DragEnd.X);
			end.Y = Math.Max(DragStart.Y, DragEnd.Y);
			return end;
		}
	}
}
