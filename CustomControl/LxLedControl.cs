using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CustomControl
{
    public class LxLedControl : Control, ISupportInitialize
    {
        private const float WIDTHHEIGHTRATIO = 0.5f;
        private GraphicsPath[] m_CachedPaths = new GraphicsPath[8];
        private bool m_bIsCacheBuild;
        private int m_nBorderWidth = 1;
        private Color m_colBorderColor = Color.Gray;
        private Color m_colFocusedBorderColor = Color.Cyan;
        private int m_nCornerRadius = 5;
        private int m_nCharacterNumber = 5;
        private float m_fBevelRate = 0.25f;
        private Color m_colFadedColor = Color.DimGray;
        private Color m_colCustomBk1 = Color.Black;
        private Color m_colCustomBk2 = Color.DimGray;
        private float m_fWidthSegWidthRatio = 0.2f;
        private float m_fWidthIntervalRatio = 0.05f;
        private LxLedControl.Alignment m_enumAlign;
        private bool m_bRoundRect;
        private bool m_bGradientBackground;
        private bool m_bShowHighlight;
        private byte m_nHighlightOpaque = 50;
        private bool m_smoothingMode;
        private bool m_italicMode;
        private bool m_bIsInitializing;

        public LxLedControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            ForeColor = Color.LightGreen;
            BackColor = Color.Transparent;
            Click += new EventHandler(EvClick);
            KeyDown += new KeyEventHandler(EvKeyDown);
            GotFocus += new EventHandler(EvFocus);
            LostFocus += new EventHandler(EvFocus);
        }

        protected override void Dispose(bool disposing)
        {
            DestoryCache();
            base.Dispose(disposing);
        }

        private void DrawSegment(
          Graphics g,
          Rectangle rectBound,
          Color colSegment,
          int nIndex,
          float bevelRate,
          float segmentWidth,
          float segmentInterval)
        {
            if (!m_bIsCacheBuild)
            {
                DestoryCache();
                CreateCache(rectBound, bevelRate, segmentWidth, segmentInterval);
            }
            GraphicsPath path = (GraphicsPath)m_CachedPaths[nIndex - 1].Clone();
            Matrix matrix = new Matrix();
            matrix.Translate((float)rectBound.X, (float)rectBound.Y);
            path.Transform(matrix);
            SolidBrush solidBrush = new SolidBrush(colSegment);
            g.Clip = new Region(ClientRectangle);
            g.FillPath((Brush)solidBrush, path);
            solidBrush.Dispose();
            matrix.Dispose();
            path.Dispose();
        }

        private void DrawSingleChar(
          Graphics g,
          Rectangle rectBound,
          Color colCharacter,
          char character,
          float bevelRate,
          float segmentWidth,
          float segmentInterval)
        {
            switch (character)
            {
                case '(':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case ')':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '-':
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '.':
                    DrawSegment(g, rectBound, colCharacter, 8, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '0':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '1':
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '2':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '3':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '4':
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '5':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '6':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '7':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '8':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '9':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'A':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'B':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'C':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'D':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'E':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'F':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'G':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'H':
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'I':
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'J':
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'L':
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'N':
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'O':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'P':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'R':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'S':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'T':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'U':
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'V':
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'Y':
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'Z':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '_':
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
            }
        }

        private void DrawSingleCharWithFadedBk(
          Graphics g,
          Rectangle rectBound,
          Color colCharacter,
          Color colFaded,
          char character,
          float bevelRate,
          float segmentWidth,
          float segmentInterval)
        {
            switch (character)
            {
                case '(':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case ')':
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '-':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '.':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 8, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '0':
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '1':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '2':
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '3':
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '4':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '5':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '6':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '7':
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '8':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '9':
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'A':
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'B':
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'C':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'D':
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'E':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'F':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'G':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'H':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'I':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'J':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'L':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'N':
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'O':
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'P':
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'R':
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'S':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'T':
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'U':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'V':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'Y':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case 'Z':
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                case '_':
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colCharacter, 4, bevelRate, segmentWidth, segmentInterval);
                    break;
                default:
                    DrawSegment(g, rectBound, colFaded, 1, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 2, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 3, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 4, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 5, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 6, bevelRate, segmentWidth, segmentInterval);
                    DrawSegment(g, rectBound, colFaded, 7, bevelRate, segmentWidth, segmentInterval);
                    break;
            }
        }

        private void DestoryCache()
        {
            for (int index = 0; index < 8; ++index)
            {
                if (m_CachedPaths[index] != null)
                {
                    m_CachedPaths[index].Dispose();
                    m_CachedPaths[index] = (GraphicsPath)null;
                }
            }
        }

        private void CreateCache(
          Rectangle rectBound,
          float bevelRate,
          float segmentWidth,
          float segmentInterval)
        {
            Matrix matrix = new Matrix(1f, 0.0f, 0.0f, 1f, 0.0f, 0.0f);
            matrix.Shear(-0.1f, 0.0f);
            PointF[] points1 = new PointF[6];
            PointF[] points2 = new PointF[4];
            for (int index = 0; index < 8; ++index)
            {
                if (m_CachedPaths[index] == null)
                    m_CachedPaths[index] = new GraphicsPath();
            }
            points1[0].X = segmentWidth * bevelRate + segmentInterval;
            points1[0].Y = segmentWidth * bevelRate;
            points1[1].X = segmentInterval + (float)((double)segmentWidth * (double)bevelRate * 2.0);
            points1[1].Y = 0.0f;
            points1[2].X = (float)((double)rectBound.Width - (double)segmentInterval - (double)segmentWidth * (double)bevelRate * 2.0);
            points1[2].Y = 0.0f;
            points1[3].X = (float)((double)rectBound.Width - (double)segmentInterval - (double)segmentWidth * (double)bevelRate);
            points1[3].Y = segmentWidth * bevelRate;
            points1[4].X = (float)rectBound.Width - segmentInterval - segmentWidth;
            points1[4].Y = segmentWidth;
            points1[5].X = segmentWidth + segmentInterval;
            points1[5].Y = segmentWidth;
            m_CachedPaths[0].AddPolygon(points1);
            m_CachedPaths[0].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[0].Transform(matrix);
            points1[0].X = (float)rectBound.Width - segmentWidth;
            points1[0].Y = segmentWidth + segmentInterval;
            points1[1].X = (float)rectBound.Width - segmentWidth * bevelRate;
            points1[1].Y = segmentWidth * bevelRate + segmentInterval;
            points1[2].X = (float)(rectBound.Width + 1);
            points1[2].Y = (float)((double)segmentWidth * (double)bevelRate * 2.0 + (double)segmentInterval + 1.0);
            points1[3].X = (float)(rectBound.Width + 1);
            points1[3].Y = (float)((double)(rectBound.Height >> 1) - (double)segmentWidth * 0.5 - (double)segmentInterval - 1.0);
            points1[4].X = (float)rectBound.Width - segmentWidth * 0.5f;
            points1[4].Y = (float)(rectBound.Height >> 1) - segmentInterval;
            points1[5].X = (float)rectBound.Width - segmentWidth;
            points1[5].Y = (float)(rectBound.Height >> 1) - segmentWidth * 0.5f - segmentInterval;
            m_CachedPaths[1].AddPolygon(points1);
            m_CachedPaths[1].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[1].Transform(matrix);
            points1[0].X = (float)rectBound.Width - segmentWidth;
            points1[0].Y = (float)((double)(rectBound.Height >> 1) + (double)segmentInterval + (double)segmentWidth * 0.5);
            points1[1].X = (float)rectBound.Width - segmentWidth * 0.5f;
            points1[1].Y = (float)(rectBound.Height >> 1) + segmentInterval;
            points1[2].X = (float)(rectBound.Width + 1);
            points1[2].Y = (float)((double)(rectBound.Height >> 1) + (double)segmentInterval + (double)segmentWidth * 0.5 + 1.0);
            points1[3].X = (float)(rectBound.Width + 1);
            points1[3].Y = (float)((double)rectBound.Height - (double)segmentInterval - (double)segmentWidth * (double)bevelRate * 2.0 - 1.0);
            points1[4].X = (float)rectBound.Width - segmentWidth * bevelRate;
            points1[4].Y = (float)rectBound.Height - segmentWidth * bevelRate - segmentInterval;
            points1[5].X = (float)rectBound.Width - segmentWidth;
            points1[5].Y = (float)rectBound.Height - segmentWidth - segmentInterval;
            m_CachedPaths[2].AddPolygon(points1);
            m_CachedPaths[2].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[2].Transform(matrix);
            points1[0].X = segmentWidth * bevelRate + segmentInterval;
            points1[0].Y = (float)rectBound.Height - segmentWidth * bevelRate;
            points1[1].X = segmentWidth + segmentInterval;
            points1[1].Y = (float)rectBound.Height - segmentWidth;
            points1[2].X = (float)rectBound.Width - segmentInterval - segmentWidth;
            points1[2].Y = (float)rectBound.Height - segmentWidth;
            points1[3].X = (float)((double)rectBound.Width - (double)segmentInterval - (double)segmentWidth * (double)bevelRate);
            points1[3].Y = (float)rectBound.Height - segmentWidth * bevelRate;
            points1[4].X = (float)((double)rectBound.Width - (double)segmentInterval - (double)segmentWidth * (double)bevelRate * 2.0);
            points1[4].Y = (float)rectBound.Height;
            points1[5].X = (float)((double)segmentWidth * (double)bevelRate * 2.0) + segmentInterval;
            points1[5].Y = (float)rectBound.Height;
            m_CachedPaths[3].AddPolygon(points1);
            m_CachedPaths[3].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[3].Transform(matrix);
            points1[0].X = 0.0f;
            points1[0].Y = (float)((double)(rectBound.Height >> 1) + (double)segmentInterval + (double)segmentWidth * 0.5);
            points1[1].X = segmentWidth * 0.5f;
            points1[1].Y = (float)(rectBound.Height >> 1) + segmentInterval;
            points1[2].X = segmentWidth;
            points1[2].Y = (float)((double)(rectBound.Height >> 1) + (double)segmentInterval + (double)segmentWidth * 0.5);
            points1[3].X = segmentWidth;
            points1[3].Y = (float)rectBound.Height - segmentWidth - segmentInterval;
            points1[4].X = segmentWidth * bevelRate;
            points1[4].Y = (float)rectBound.Height - segmentWidth * bevelRate - segmentInterval;
            points1[5].X = 0.0f;
            points1[5].Y = (float)((double)rectBound.Height - (double)segmentInterval - (double)segmentWidth * (double)bevelRate * 2.0);
            m_CachedPaths[4].AddPolygon(points1);
            m_CachedPaths[4].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[4].Transform(matrix);
            points1[0].X = 0.0f;
            points1[0].Y = (float)((double)segmentWidth * (double)bevelRate * 2.0) + segmentInterval;
            points1[1].X = segmentWidth * bevelRate;
            points1[1].Y = segmentWidth * bevelRate + segmentInterval;
            points1[2].X = segmentWidth;
            points1[2].Y = segmentWidth + segmentInterval;
            points1[3].X = segmentWidth;
            points1[3].Y = (float)(rectBound.Height >> 1) - segmentWidth * 0.5f - segmentInterval;
            points1[4].X = segmentWidth * 0.5f;
            points1[4].Y = (float)(rectBound.Height >> 1) - segmentInterval;
            points1[5].X = 0.0f;
            points1[5].Y = (float)(rectBound.Height >> 1) - segmentWidth * 0.5f - segmentInterval;
            m_CachedPaths[5].AddPolygon(points1);
            m_CachedPaths[5].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[5].Transform(matrix);
            points1[0].X = segmentWidth * 0.5f + segmentInterval;
            points1[0].Y = (float)(rectBound.Height >> 1);
            points1[1].X = segmentWidth + segmentInterval;
            points1[1].Y = (float)(rectBound.Height >> 1) - segmentWidth * 0.5f;
            points1[2].X = (float)rectBound.Width - segmentInterval - segmentWidth;
            points1[2].Y = (float)(rectBound.Height >> 1) - segmentWidth * 0.5f;
            points1[3].X = (float)((double)rectBound.Width - (double)segmentInterval - (double)segmentWidth * 0.5);
            points1[3].Y = (float)(rectBound.Height >> 1);
            points1[4].X = (float)rectBound.Width - segmentInterval - segmentWidth;
            points1[4].Y = (float)(rectBound.Height >> 1) + segmentWidth * 0.5f;
            points1[5].X = segmentWidth + segmentInterval;
            points1[5].Y = (float)(rectBound.Height >> 1) + segmentWidth * 0.5f;
            m_CachedPaths[6].AddPolygon(points1);
            m_CachedPaths[6].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[6].Transform(matrix);
            points2[0].X = 0.0f;
            points2[0].Y = (float)rectBound.Height;
            points2[1].X = segmentWidth;
            points2[1].Y = (float)rectBound.Height;
            points2[2].X = segmentWidth;
            points2[2].Y = (float)rectBound.Height - segmentWidth;
            points2[3].X = 0.0f;
            points2[3].Y = (float)rectBound.Height - segmentWidth;
            m_CachedPaths[7].AddPolygon(points2);
            m_CachedPaths[7].CloseFigure();
            if (UseItalicStyle)
                m_CachedPaths[7].Transform(matrix);
            m_bIsCacheBuild = true;
        }

        private void DrawChars(Graphics g, float segmentWidth, float segmentInterval)
        {
            Rectangle clientRectangle = ClientRectangle;
            Rectangle rectBound = new Rectangle();
            int num1 = (int)((double)clientRectangle.Height * 0.5);
            int height = clientRectangle.Height;
            int nCharacterNumber = m_nCharacterNumber;
            int num2 = (double)segmentInterval > 0.5 ? (int)((double)segmentInterval * 2.0) : 1;
            int num3 = 0;
            if (m_enumAlign == LxLedControl.Alignment.Right)
                num3 = Text.Length < nCharacterNumber ? nCharacterNumber - Text.Length : 0;
            for (int index = 0; index < nCharacterNumber; ++index)
            {
                rectBound.Width = num1;
                rectBound.Height = height;
                rectBound.X = index * rectBound.Width + 5;
                rectBound.Y = 0;
                rectBound.Inflate(-num2, -num2);
                if (m_enumAlign == LxLedControl.Alignment.Left)
                {
                    if (index < Text.Length)
                        DrawSingleCharWithFadedBk(g, rectBound, ForeColor, m_colFadedColor, Text[index], m_fBevelRate, segmentWidth, segmentInterval);
                    else
                        DrawSingleChar(g, rectBound, m_colFadedColor, '8', m_fBevelRate, segmentWidth, segmentInterval);
                }
                else if (index >= num3)
                    DrawSingleCharWithFadedBk(g, rectBound, ForeColor, m_colFadedColor, Text[index - num3], m_fBevelRate, segmentWidth, segmentInterval);
                else
                    DrawSingleChar(g, rectBound, m_colFadedColor, '8', m_fBevelRate, segmentWidth, segmentInterval);
            }
        }

        private void CalculateDrawingParams(out float segmentWidth, out float segmentInterval)
        {
            float num = (float)ClientRectangle.Height * 0.5f;
            segmentWidth = num * m_fWidthSegWidthRatio;
            segmentInterval = num * m_fWidthIntervalRatio;
        }

        private void DrawRoundRect(
          Graphics g,
          Rectangle rect,
          float radius,
          Color col1,
          Color col2,
          Color colBorder,
          int nBorderWidth,
          bool bGradient,
          bool bDrawBorder)
        {
            GraphicsPath path = new GraphicsPath();
            float num = radius + radius;
            RectangleF rect1 = new RectangleF(0.0f, 0.0f, num, num);
            rect1.X = (float)rect.Left;
            rect1.Y = (float)rect.Top;
            path.AddArc(rect1, 180f, 90f);
            rect1.X = (float)(rect.Right - 1) - num;
            path.AddArc(rect1, 270f, 90f);
            rect1.Y = (float)(rect.Bottom - 1) - num;
            path.AddArc(rect1, 0.0f, 90f);
            rect1.X = (float)rect.Left;
            path.AddArc(rect1, 90f, 90f);
            path.CloseFigure();
            Brush brush = !bGradient ? (Brush)new SolidBrush(col1) : (Brush)new LinearGradientBrush(rect, col1, col2, 90f, false);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPath(brush, path);
            if (bDrawBorder)
            {
                Pen pen = new Pen(colBorder);
                pen.Width = (float)nBorderWidth;
                g.DrawPath(pen, path);
                pen.Dispose();
            }
            g.SmoothingMode = UseSmoothingMode ? SmoothingMode.AntiAlias : SmoothingMode.None;
            brush.Dispose();
            path.Dispose();
        }

        private void DrawNormalRect(
          Graphics g,
          Rectangle rect,
          Color col1,
          Color col2,
          Color colBorder,
          int nBorderWidth,
          bool bGradient,
          bool bDrawBorder)
        {
            Brush brush;
            if (bGradient)
            {
                brush = (Brush)new LinearGradientBrush(rect, col1, col2, 90f);
                g.FillRectangle(brush, rect);
            }
            else
            {
                brush = (Brush)new SolidBrush(col1);
                g.FillRectangle(brush, rect);
            }
            if (bDrawBorder)
            {
                --rect.Width;
                --rect.Height;
                Pen pen = new Pen(colBorder);
                g.DrawRectangle(pen, rect);
                pen.Dispose();
            }
            brush.Dispose();
        }

        private void DrawBackground(Graphics g)
        {
            Rectangle clientRectangle = ClientRectangle;
            Color colBorder = Focused ? m_colFocusedBorderColor : m_colBorderColor;
            if (m_bRoundRect)
            {
                DrawRoundRect(g, clientRectangle, (float)m_nCornerRadius, m_colCustomBk1, m_colCustomBk2, colBorder, m_nBorderWidth, m_bGradientBackground, m_nBorderWidth != 0);
            }
            else
            {
                if (m_colCustomBk1 == Color.Transparent)
                    return;
                DrawNormalRect(g, clientRectangle, m_colCustomBk1, m_colCustomBk2, colBorder, m_nBorderWidth, m_bGradientBackground, m_nBorderWidth != 0);
            }
        }

        private void DrawHighlight(Graphics g)
        {
            if (!m_bShowHighlight)
                return;
            Rectangle clientRectangle = ClientRectangle;
            clientRectangle.Height >>= 1;
            clientRectangle.Inflate(-2, -2);
            Color col1 = Color.FromArgb(100, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue);
            Color col2 = Color.FromArgb((int)m_nHighlightOpaque, (int)byte.MaxValue, (int)byte.MaxValue, (int)byte.MaxValue);
            DrawRoundRect(g, clientRectangle, m_nCornerRadius - 1 > 1 ? (float)(m_nCornerRadius - 1) : 1f, col1, col2, Color.Empty, 0, true, false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = UseSmoothingMode ? SmoothingMode.AntiAlias : SmoothingMode.None;
            float segmentWidth = 0.0f;
            float segmentInterval = 0.0f;
            if (ClientRectangle.Height < 20 || ClientRectangle.Width < 20)
                return;
            DrawBackground(graphics);
            CalculateDrawingParams(out segmentWidth, out segmentInterval);
            DrawChars(graphics, segmentWidth, segmentInterval);
            DrawHighlight(graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent) => base.OnPaintBackground(pevent);

        protected override void OnSizeChanged(EventArgs e)
        {
            m_bIsCacheBuild = false;
            base.OnSizeChanged(e);
        }

        [DefaultValue(false)]
        [Category("Appearance")]
        [Browsable(true)]
        [Description("Turn on/off the italic text style.")]
        public bool UseItalicStyle
        {
            get => m_italicMode;
            set
            {
                if (m_italicMode == value)
                    return;
                m_italicMode = value;
                m_bIsCacheBuild = false;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Description("Turn on/off the smoothing mode.")]
        [DefaultValue(false)]
        [Browsable(true)]
        [Category("Appearance")]
        public bool UseSmoothingMode
        {
            get => m_smoothingMode;
            set
            {
                if (m_smoothingMode == value)
                    return;
                m_smoothingMode = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(1)]
        [Category("Appearance")]
        [Description("Set the border style")]
        public int BorderWidth
        {
            get => m_nBorderWidth;
            set
            {
                if (m_nBorderWidth == value)
                    return;
                m_nBorderWidth = value >= 0 && value <= 5 ? value : throw new ArgumentException("This value should be between 0 and 5");
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Description("Set the border color")]
        [Browsable(true)]
        [DefaultValue(typeof(Color), "Gray")]
        [Category("Appearance")]
        public Color BorderColor
        {
            get => m_colBorderColor;
            set
            {
                if (value == m_colBorderColor)
                    return;
                m_colBorderColor = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Description("Set the focused border color.")]
        [DefaultValue(typeof(Color), "Cyan")]
        [Category("Appearance")]
        public Color FocusedBorderColor
        {
            get => m_colFocusedBorderColor;
            set
            {
                if (value == m_colFocusedBorderColor)
                    return;
                m_colFocusedBorderColor = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(50)]
        [Category("Appearance")]
        [Description("Set the opaque value of the highlight")]
        public byte HighlightOpaque
        {
            get => m_nHighlightOpaque;
            set
            {
                if (value > (byte)100)
                    throw new ArgumentException("This value should be between 0 and 50");
                if ((int)m_nHighlightOpaque == (int)value)
                    return;
                m_nHighlightOpaque = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [DefaultValue(false)]
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Set whether to show highlight area on the control")]
        public bool ShowHighlight
        {
            get => m_bShowHighlight;
            set
            {
                if (m_bShowHighlight == value)
                    return;
                m_bShowHighlight = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(5)]
        [Category("Appearance")]
        [Description("Set the corner radius for the background rectangle.")]
        public int CornerRadius
        {
            get => m_nCornerRadius;
            set
            {
                if (value < 1 || value > 10)
                    throw new ArgumentException("This value should be between 1 and 10");
                if (m_nCornerRadius == value)
                    return;
                m_nCornerRadius = value;
                if (!m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Description("Set if the background was filled in gradient colors")]
        [DefaultValue(false)]
        [Browsable(true)]
        [Category("Appearance")]
        public bool GradientBackground
        {
            get => m_bGradientBackground;
            set
            {
                if (m_bGradientBackground == value)
                    return;
                m_bGradientBackground = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Description("Set thr first custom background color")]
        [DefaultValue(typeof(Color), "System.Drawing.Color.Black")]
        [Category("Appearance")]
        public Color BackColor_1
        {
            get => m_colCustomBk1;
            set
            {
                m_colCustomBk1 = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "System.Drawing.Color.DimGray")]
        [Description("Set thr second custom background color")]
        [Browsable(true)]
        [Category("Appearance")]
        public Color BackColor_2
        {
            get => m_colCustomBk2;
            set
            {
                m_colCustomBk2 = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [Browsable(true)]
        [Description("Set the background bound style")]
        public bool RoundCorner
        {
            get => m_bRoundRect;
            set
            {
                if (m_bRoundRect == value)
                    return;
                m_bRoundRect = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [Browsable(true)]
        [DefaultValue(40)]
        [Description("Set segment interval ratio")]
        public int SegmentIntervalRatio
        {
            get => (int)(((double)m_fWidthIntervalRatio - 0.0099999997764825821) * 1000.0);
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentException("This value should be between 0 and 100");
                m_fWidthIntervalRatio = (float)(0.0099999997764825821 + (double)value * (1.0 / 1000.0));
                if (m_bIsInitializing)
                    return;
                m_bIsCacheBuild = false;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(typeof(LxLedControl.Alignment), "Left")]
        [Category("Appearance")]
        [Description("Set the alignment of the text")]
        public LxLedControl.Alignment TextAlignment
        {
            get => m_enumAlign;
            set
            {
                m_enumAlign = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("Set the segment width ratio")]
        [DefaultValue(50)]
        public int SegmentWidthRatio
        {
            get => (int)(((double)m_fWidthSegWidthRatio - 0.10000000149011612) * 500.0);
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentException("This value should be between 0 and 100");
                m_fWidthSegWidthRatio = (float)(0.10000000149011612 + (double)value * (1.0 / 500.0));
                if (m_bIsInitializing)
                    return;
                m_bIsCacheBuild = false;
                Invalidate();
            }
        }

        [Description("Set the total number of characters to display")]
        [DefaultValue(5)]
        [Category("Behavior")]
        [Browsable(true)]
        public int TotalCharCount
        {
            get => m_nCharacterNumber;
            set
            {
                m_nCharacterNumber = value >= 2 ? value : throw new ArgumentException("This value should be greater than 2.");
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(0.25)]
        [Category("Behavior")]
        [Description("Set the bevel rate of each segment")]
        public float BevelRate
        {
            get => m_fBevelRate * 2f;
            set
            {
                if ((double)value < 0.0 || (double)value > 1.0)
                    throw new ArgumentException("This value should be between 0.0 and 1");
                m_fBevelRate = value / 2f;
                if (m_bIsInitializing)
                    return;
                m_bIsCacheBuild = false;
                Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "System.Color.DimGray")]
        [Description("Set the color of background characters")]
        [Browsable(true)]
        [Category("Appearance")]
        public Color FadedColor
        {
            get => m_colFadedColor;
            set
            {
                if (m_colFadedColor == value)
                    return;
                m_colFadedColor = value;
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Description("Set text of the control")]
        [Category("Appearance")]
        [DefaultValue("HELLO")]
        [Browsable(true)]
        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value.ToUpper();
                if (m_bIsInitializing)
                    return;
                Invalidate();
            }
        }

        [Browsable(false)]
        public override Image BackgroundImage
        {
            get => base.BackgroundImage;
            set => base.BackgroundImage = (Image)null;
        }

        [Browsable(false)]
        public override ImageLayout BackgroundImageLayout
        {
            get => base.BackgroundImageLayout;
            set
            {
            }
        }

        [Browsable(false)]
        public override Font Font
        {
            get => base.Font;
            set
            {
            }
        }

        void ISupportInitialize.BeginInit() => m_bIsInitializing = true;

        void ISupportInitialize.EndInit()
        {
            m_bIsInitializing = false;
            Invalidate();
        }

        private void EvClick(object sender, EventArgs e) => Focus();

        private void EvKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control)
                return;
            if (e.KeyCode != Keys.C)
                return;
            try
            {
                Clipboard.SetText(Text);
            }
            catch
            {
            }
        }

        private void EvFocus(object sender, EventArgs e) => Invalidate();

        private void InitializeComponent()
        {
            SuspendLayout();
            ResumeLayout(false);
        }

        public enum Alignment
        {
            Left,
            Right,
        }
    }
}
