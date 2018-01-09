﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Represents a composite Model3D.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf.SharpDX
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows.Markup;
    using System.Linq;
    using global::SharpDX;
    using System;
    using global::SharpDX.Direct3D11;
    using Core;
    using System.Windows;

    /// <summary>
    ///     Represents a composite Model3D.
    /// </summary>
    [ContentProperty("Children")]
    public class CompositeModel3D : Element3D, IHitable, ISelectable, IMouse3D
    {
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(CompositeModel3D), new AffectsRenderPropertyMetadata(false));

        public bool IsSelected
        {
            get
            {
                return (bool)this.GetValue(IsSelectedProperty);
            }
            set
            {
                this.SetValue(IsSelectedProperty, value);
            }
        }

        private readonly ObservableElement3DCollection children;

        public override IEnumerable<IRenderable> Items { get { return children; } }

        #region Events
        public static readonly RoutedEvent MouseDown3DEvent =
            EventManager.RegisterRoutedEvent("MouseDown3D", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GeometryModel3D));

        public static readonly RoutedEvent MouseUp3DEvent =
            EventManager.RegisterRoutedEvent("MouseUp3D", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GeometryModel3D));

        public static readonly RoutedEvent MouseMove3DEvent =
            EventManager.RegisterRoutedEvent("MouseMove3D", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(GeometryModel3D));

        /// <summary>
        /// Provide CLR accessors for the event 
        /// </summary>
        public event RoutedEventHandler MouseDown3D
        {
            add { AddHandler(MouseDown3DEvent, value); }
            remove { RemoveHandler(MouseDown3DEvent, value); }
        }

        /// <summary>
        /// Provide CLR accessors for the event 
        /// </summary>
        public event RoutedEventHandler MouseUp3D
        {
            add { AddHandler(MouseUp3DEvent, value); }
            remove { RemoveHandler(MouseUp3DEvent, value); }
        }

        /// <summary>
        /// Provide CLR accessors for the event 
        /// </summary>
        public event RoutedEventHandler MouseMove3D
        {
            add { AddHandler(MouseMove3DEvent, value); }
            remove { RemoveHandler(MouseMove3DEvent, value); }
        }
        #endregion
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompositeModel3D" /> class.
        /// </summary>
        public CompositeModel3D()
        {
            this.children = new ObservableElement3DCollection();
            this.children.CollectionChanged += this.ChildrenChanged;
            this.MouseDown3D += OnMouse3DDown;
            this.MouseUp3D += OnMouse3DUp;
            this.MouseMove3D += OnMouse3DMove;
        }

        public virtual void OnMouse3DDown(object sender, RoutedEventArgs e) { }

        public virtual void OnMouse3DUp(object sender, RoutedEventArgs e) { }

        public virtual void OnMouse3DMove(object sender, RoutedEventArgs e) { }
        /// <summary>
        ///     Gets the children.
        /// </summary>
        /// <value>
        ///     The children.
        /// </value>
        public ObservableElement3DCollection Children { get { return this.children; } }

        /// <summary>
        /// Attaches the specified host.
        /// </summary>
        /// <param name="host">
        /// The host.
        /// </param>
        protected override bool OnAttach(IRenderHost host)
        {
            foreach (var model in this.Children)
            {
                if (model.Parent == null)
                {
                    this.AddLogicalChild(model);
                }

                model.Attach(host);
            }
            return true;
        }

        /// <summary>
        ///     Detaches this instance.
        /// </summary>
        protected override void OnDetach()
        {
            foreach (var model in this.Children)
            {
                model.Detach();
                if (model.Parent == this)
                {
                    this.RemoveLogicalChild(model);
                }
            }
            base.OnDetach();
        }

        //protected override bool CanRender(IRenderContext context)
        //{
        //    return IsAttached && isRenderingInternal && visibleInternal;
        //}
        /// <summary>
        /// Renders the specified context.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        protected override void OnRender(IRenderContext context)
        {
            // you mean like this?
            foreach (var model in this.Children)
            {
                model.ParentMatrix = this.TotalModelMatrix;
                // push matrix                    
                //model.PushMatrix(this.modelMatrix);
                // render model
                model.Render(context);
                // pop matrix                   
                //model.PopMatrix();
            }
        }

        /// <summary>
        /// Handles changes in the Children collection.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.
        /// </param>
        private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        foreach (Element3D item in e.OldItems)
                        {
                            // todo: detach?
                            // yes, always
                            item.Detach();
                            if (item.Parent == this)
                            {
                                this.RemoveLogicalChild(item);
                            }
                        }
                        break;
                }
            }

            if (e.NewItems != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Replace:
                        foreach (Element3D item in e.NewItems)
                        {
                            if (this.IsAttached)
                            {
                                // todo: attach?
                                // yes, always  
                                // where to get a refrence to renderHost?
                                // store it as private memeber of the class?
                                if (item.Parent == null)
                                {
                                    this.AddLogicalChild(item);
                                }

                                item.Attach(RenderHost);
                            }
                        }
                        break;
                }
            }
        }

        protected override bool CanHitTest(IRenderContext context)
        {
            return base.CanHitTest(context) && Children.Count > 0;
        }

        protected override bool OnHitTest(IRenderContext context, Matrix totalModelMatrix, ref Ray ray, ref List<HitTestResult> hits)
        {
            bool hit = false;
            foreach (var c in this.Children)
            {
                var hc = c as IHitable;
                if (hc != null)
                {
                    hc.HitTest(context, ray, ref hits);
                    //var tc = c as ITransformable;
                    //if (tc != null)
                    //{
                    //    //tc.PushMatrix(this.modelMatrix);
                    //    if (hc.HitTest(context, ray, ref hits))
                    //    {
                    //        hit = true;
                    //    }
                    //   // tc.PopMatrix();
                    //}
                    //else
                    //{
                    //    if (hc.HitTest(context, ray, ref hits))
                    //    {
                    //        hit = true;
                    //    }
                    //}
                }
            }
            if (hit)
            {
                var pos = ray.Position;
                hits = hits.OrderBy(x => Vector3.DistanceSquared(pos, x.PointHit)).ToList();
            }
            return hit;
        }
    }
}