﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VirtualKeyboard.Converters
{
    //----------------------------------------------------------------------------
    // File: ProgressBarBrushConverter.cs
    //
    // Description:
    // Converts a brush into a DrawingBrush used to display the "block" style
    // progress bar
    //
    // History:
    //  06/28/2004 - t-sergin - Created
    //
    // Copyright (C) 2004,2005 by Microsoft Corporation.  All rights reserved.
    //
    //---------------------------------------------------------------------------
    /// <summary>
    /// The ProgressBarBrushConverter class
    /// </summary>
    public class ProgressBarBrushConverter : IMultiValueConverter
    {
        /// <summary>
        ///      Creates the brush for the ProgressBar
        /// </summary>
        /// <param name="values">ForegroundBrush, IsIndeterminate, Indicator Width, Indicator Height, Track Width
        /// <param name="targetType">
        /// <param name="parameter">
        /// <param name="culture">
        /// <returns>Brush for the ProgressBar</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //
            // Parameter Validation
            //
            Type doubleType = typeof(double);
            if (values == null ||
                (values.Length != 5) ||
                (values[0] == null) ||
                (values[1] == null) ||
                (values[2] == null) ||
                (values[3] == null) ||
                (values[4] == null) ||
                !typeof(Brush).IsAssignableFrom(values[0].GetType()) ||
                !typeof(bool).IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()) ||
                !doubleType.IsAssignableFrom(values[3].GetType()) ||
                !doubleType.IsAssignableFrom(values[4].GetType()))
            {
                return null;
            }

            // Conversion

            Brush brush = (Brush)values[0];
            bool isIndeterminate = (bool)values[1];
            double width = (double)values[2];
            double height = (double)values[3];
            double trackWidth = (double)values[4];

            // if an invalid height, return a null brush
            if (width <= 0.0 || Double.IsInfinity(width) || Double.IsNaN(width) ||
                height <= 0.0 || Double.IsInfinity(height) || Double.IsNaN(height))
            {
                return null;
            }

            DrawingBrush newBrush = new DrawingBrush();

            // Set the viewport and viewbox to the size of the progress region
            newBrush.Viewport = newBrush.Viewbox = new Rect(0, 0, width, height);
            newBrush.ViewportUnits = newBrush.ViewboxUnits = BrushMappingMode.Absolute;

            newBrush.TileMode = TileMode.None;
            newBrush.Stretch = Stretch.None;

            DrawingGroup myDrawing = new DrawingGroup();
            DrawingContext myDrawingContext = myDrawing.Open();

            double drawnWidth = 0.0; // The total width drawn to the brush so far

            double blockWidth = 6.0;
            double blockGap = -2.0; // <-- was 2.0
            double blockTotal = blockWidth + blockGap;

            // For the indeterminate case, just draw a portion of the width
            // And animate the brush
            if (isIndeterminate)
            {
                int blocks = (int)Math.Ceiling(width / blockTotal);

                // The left (X) starting point of the brush
                double left = -blocks * blockTotal;

                // Only draw 30% of the blocks
                double indeterminateWidth = width * .3;

                // Generate the brush so it wraps correctly
                // The brush is larger than the rectangle to fill like so:
                //                +-------------+
                // [] [] [] __ __ |[] [] [] __ _|
                //                +-------------+
                // Translate Brush =>>
                // To have the marquee line up on the left as the blocks are scrolled off to the right
                // we need to have the second set of blocks offset from the first by the width of the rect
                newBrush.Viewport = newBrush.Viewbox = new Rect(left, 0, indeterminateWidth - left, height);

                // Add an animated translate transfrom
                TranslateTransform translation = new TranslateTransform();

                double milliseconds = blocks * 100; // 100 milliseconds on each position

                DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
                animation.Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));  // Repeat every 3 seconds
                animation.RepeatBehavior = RepeatBehavior.Forever;

                // Add a keyframe to translate by each block
                for (int i = 1; i <= blocks; i++)
                {
                    double x = i * blockTotal;
                    animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(x, KeyTime.Uniform));
                }

                // Set the animation to the XProperty
                translation.BeginAnimation(TranslateTransform.XProperty, animation);

                // Set the animated translation on the brush
                newBrush.Transform = translation;

                // Draw the Blocks to the left of the brush that are translated into view
                // during the animation

                // While able to draw complete blocks,
                while ((drawnWidth + blockWidth) < indeterminateWidth)
                {
                    // Draw a block
                    myDrawingContext.DrawRectangle(brush, null, new Rect(left + drawnWidth, 0, blockWidth, height));
                    drawnWidth += blockTotal;
                }

                width = indeterminateWidth; //only need to draw 30% of the blocks
                drawnWidth = 0.0; //reset drawn width and draw the left blocks
            }

            // Draw as many blocks
            // While able to draw complete blocks,
            while ((drawnWidth + blockWidth) < width)
            {
                var rect = new Rect(drawnWidth, 0, blockWidth, height);

                // Snap rect to pixels
                GuidelineSet guidelines = new GuidelineSet();
                guidelines.GuidelinesX.Add(rect.Left);
                guidelines.GuidelinesX.Add(rect.Right);
                guidelines.GuidelinesY.Add(rect.Top);
                guidelines.GuidelinesY.Add(rect.Bottom);

                myDrawingContext.PushGuidelineSet(guidelines);

                // Draw a block
                myDrawingContext.DrawRectangle(brush, null, rect);
                drawnWidth += blockTotal;
            }

            double remainder = width - drawnWidth;
            // Draw portion of last block when ProgressBar is 100% (ie indicatorWidth == trackWidth)
            if (!isIndeterminate && remainder > 0.0 && Math.Abs(width - trackWidth) < 1.0e-5)
            {
                // Draw incomplete block to fill progress indicator area
                myDrawingContext.DrawRectangle(brush, null, new Rect(drawnWidth, 0, remainder, height));
            }

            myDrawingContext.Close();
            newBrush.Drawing = myDrawing;

            return newBrush;
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        /// <param name="value">value, as produced by target
        /// <param name="targetTypes">target types
        /// <param name="parameter">converter parameter
        /// <param name="culture">culture information
        /// <returns>Nothing</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
