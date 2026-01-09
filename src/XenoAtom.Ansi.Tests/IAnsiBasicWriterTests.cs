// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.ObjectModel;

namespace XenoAtom.Ansi.Tests;

[TestClass]
public class IAnsiBasicWriterTests
{
    [TestMethod]
    public void AnsiMarkup_CanWriteToIAnsiWriter_AndCaptureStyleTransitions()
    {
        var writer = new CapturingAnsiWriter();
        var markup = new AnsiMarkup(writer);

        markup.Write("[red]X[/]");

        var baseStyle = AnsiStyle.Default;
        var redStyle = baseStyle with { Foreground = AnsiColors.Red };

        Assert.HasCount(3, writer.Events);
        AssertTransition(writer.Events[0], baseStyle, redStyle);
        AssertText(writer.Events[1], "X");
        AssertTransition(writer.Events[2], redStyle, baseStyle);
    }

    private static void AssertText(CapturingAnsiWriter.Event evt, string expected)
    {
        if (evt is CapturingAnsiWriter.TextEvent text)
        {
            Assert.AreEqual(expected, text.Text);
            return;
        }

        Assert.Fail($"Expected {nameof(CapturingAnsiWriter.TextEvent)}, got {evt.GetType().Name}.");
    }

    private static void AssertTransition(CapturingAnsiWriter.Event evt, AnsiStyle from, AnsiStyle to)
    {
        if (evt is CapturingAnsiWriter.TransitionEvent transition)
        {
            Assert.AreEqual(from, transition.From);
            Assert.AreEqual(to, transition.To);
            return;
        }

        Assert.Fail($"Expected {nameof(CapturingAnsiWriter.TransitionEvent)}, got {evt.GetType().Name}.");
    }

    private sealed class CapturingAnsiWriter : IAnsiBasicWriter
    {
        private readonly List<Event> _events;

        public CapturingAnsiWriter()
        {
            _events = new List<Event>();
            Capabilities = AnsiCapabilities.Default;
        }

        public AnsiCapabilities Capabilities { get; }

        public ReadOnlyCollection<Event> Events => _events.AsReadOnly();

        public void Write(ReadOnlySpan<char> text)
        {
            if (!text.IsEmpty)
            {
                _events.Add(new TextEvent(text.ToString()));
            }
        }

        public void StyleTransition(AnsiStyle from, AnsiStyle to)
        {
            _events.Add(new TransitionEvent(from, to));
        }

        public abstract class Event;

        public sealed class TextEvent : Event
        {
            public TextEvent(string text) => Text = text;

            public string Text { get; }
        }

        public sealed class TransitionEvent : Event
        {
            public TransitionEvent(AnsiStyle from, AnsiStyle to)
            {
                From = from;
                To = to;
            }

            public AnsiStyle From { get; }
            public AnsiStyle To { get; }
        }
    }
}
