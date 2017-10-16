﻿using System;
using System.Text;

namespace LunarParser.JSON
{
    public class JSONReader
    {
        private enum State
        {
            Type,
            Name,
            Colon,
            Value,
            Next
        }

        private enum InputMode
        {
            None,
            Text,
            Number
        }

        public static DataNode ReadFromString(string contents)
        {
            int index = 0;
            var root = ReadNode(contents, ref index, null);
            return root;
        }

        private static string GetPos(string contents, int index)
        {
            return "offset " + index;
        }

        private static DataNode ReadNode(string contents, ref int index, string name)
        {
            DataNode result = null;

            var state = State.Type;
            char c;
            var mode = InputMode.None;

            StringBuilder name_content = new StringBuilder();
            StringBuilder value_content = new StringBuilder();

            int rewind_index = index;

            do
            {
                bool isWhiteSpace;
                bool next = false;
                do
                {
                    if (index >= contents.Length)
                    {
                        throw new Exception($"JSON parsing exception, unexpected end of data");
                    }

                    c = contents[index];
                    isWhiteSpace = Char.IsWhiteSpace(c);

                    if (!isWhiteSpace)
                    {
                        rewind_index = index;
                    }

                    index++;


                    next = (mode == InputMode.None) ? isWhiteSpace : false;
                } while (next);

                switch (state)
                {
                    case State.Type:
                        {
                            switch (c)
                            {
                                case '{':
                                    {
                                        result = DataNode.CreateObject(name);
                                        state = State.Name;
                                        break;
                                    }

                                case '[':
                                    {
                                        result = DataNode.CreateObject(name);
                                        state = State.Name;
                                        break;
                                    }


                                default:
                                    {
                                        throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, unexpected character");
                                    }
                            }
                            break;
                        }

                    case State.Name:
                        {
                            switch (c)
                            {
                                case '"':
                                    {
                                        if (mode == InputMode.None)
                                        {
                                            mode = InputMode.Text;
                                            name_content.Length = 0;
                                        }
                                        else
                                        {
                                            mode = InputMode.None;
                                            state = State.Colon;
                                        }
                                        break;
                                    }

                                default:
                                    {
                                        if (mode == InputMode.Text)
                                        {
                                            name_content.Append(c);
                                        }
                                        else
                                        {
                                            throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, unexpected character");
                                        }
                                        break;
                                    }
                            }
                            break;
                        }

                    case State.Colon:
                        {
                            switch (c)
                            {
                                case ':':
                                    {
                                        state = State.Value;
                                        break;
                                    }

                                default:
                                    {
                                        throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, expected collon");
                                    }
                            }
                            break;
                        }

                    case State.Value:
                        {
                            switch (c)
                            {
                                case '"':
                                    {
                                        if (mode == InputMode.None)
                                        {
                                            mode = InputMode.Text;
                                            value_content.Length = 0;
                                        }
                                        else
                                        {
                                            mode = InputMode.None;
                                            result.AddField(name_content.ToString(), value_content.ToString());
                                            state = State.Next;
                                        }
                                        break;
                                    }

                                case '[':
                                case '{':
                                    {
                                        index = rewind_index;
                                        var node = ReadNode(contents, ref index, name_content.ToString());
                                        result.AddNode(node);

                                        state = State.Next;
                                        break;
                                    }

                                default:
                                    {
                                        if (mode == InputMode.Text)
                                        {
                                            value_content.Append(c);
                                        }
                                        else
                                        if (char.IsNumber(c))
                                        {
                                            if (mode != InputMode.Number)
                                            {
                                                value_content.Length = 0;
                                                mode = InputMode.Number;
                                            }

                                            value_content.Append(c);
                                        }
                                        else
                                        {
                                            if (mode == InputMode.Number)
                                            {
                                                mode = InputMode.None;
                                                result.AddField(name_content.ToString(), value_content.ToString());
                                                state = State.Next;

                                                if (c == ',')
                                                {
                                                    index = rewind_index;
                                                }
                                            }
                                            else
                                            {
                                                throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, unexpected character");
                                            }

                                        }
                                        break;
                                    }
                            }
                            break;
                        }

                    case State.Next:
                        {
                            switch (c)
                            {
                                case ',':
                                    {
                                        state = State.Name;
                                        break;
                                    }

                                case '}':
                                    {
                                        if (result.Kind != NodeKind.Object)
                                        {
                                            throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, unexpected }}");
                                        }

                                        return result;
                                    }

                                case ']':
                                    {
                                        if (result.Kind != NodeKind.Array)
                                        {
                                            throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, unexpected ]");
                                        }

                                        return result;
                                    }

                                default:
                                    {
                                        throw new Exception($"JSON parsing exception at {GetPos(contents, index)}, expected collon");
                                    }
                            }
                            break;
                        }

                }

            } while (true);
        }
    }
}