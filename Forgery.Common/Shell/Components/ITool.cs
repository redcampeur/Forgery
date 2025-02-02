﻿using System.Drawing;
using Forgery.Common.Shell.Context;

namespace Forgery.Common.Shell.Components
{
    /// <summary>
    /// A tool which is hosted in the tool bar of the shell.
    /// </summary>
    public interface ITool : IContextAware
    {
        /// <summary>
        /// The tool's icon
        /// </summary>
        Image Icon { get; }

        /// <summary>
        /// The tool's name
        /// </summary>
        string Name { get; }
    }
}
