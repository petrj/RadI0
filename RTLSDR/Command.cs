using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    /// <summary>
    /// Represents a command to be sent to the SDR device.
    /// </summary>
    public class Command
    {
        private CommandsEnum _command { get; set; }
        private byte[] _arguments { get; set; }

        /// <summary>
        /// Initializes a new instance of the Command class with the specified command and arguments.
        /// </summary>
        /// <param name="command">The command type.</param>
        /// <param name="arguments">The command arguments as a byte array.</param>
        public Command(CommandsEnum command, byte[] arguments)
        {
            _command = command;
            _arguments = arguments;
        }

        /// <summary>
        /// Initializes a new instance of the Command class with the specified command and integer argument.
        /// </summary>
        /// <param name="command">The command type.</param>
        /// <param name="intArgument">The integer argument for the command.</param>
        public Command(CommandsEnum command, int intArgument)
        {
            _command = command;

            byte[] arguments = new byte[4];

            arguments[0] = (byte)((intArgument >> 24) & 0xff);
            arguments[1] = (byte)((intArgument >> 16) & 0xff);
            arguments[2] = (byte)((intArgument >> 8) & 0xff);
            arguments[3] = (byte)(intArgument & 0xff);

            _arguments = arguments;
        }

        /// <summary>
        /// Initializes a new instance of the Command class with the specified command and two short arguments.
        /// </summary>
        /// <param name="command">The command type.</param>
        /// <param name="arg1">The first short argument.</param>
        /// <param name="arg2">The second short argument.</param>
        public Command(CommandsEnum command, short arg1, short arg2)
        {
            _command = command;

            byte[] arguments = new byte[4];

            arguments[0] = (byte)((arg1 >> 8) & 0xff);
            arguments[1] = (byte)(arg1 & 0xff);
            arguments[2] = (byte)((arg2 >> 8) & 0xff);
            arguments[3] = (byte)(arg2 & 0xff);

            _arguments = arguments;
        }

        /// <summary>
        /// Returns a string representation of the command.
        /// </summary>
        /// <returns>The string representation of the command.</returns>
        public override string ToString()
        {
            return _command.ToString();
        }

        /// <summary>
        /// Converts the command to a byte array for transmission.
        /// </summary>
        /// <returns>The command as a byte array.</returns>
        public byte[] ToByteArray()
        {
            var res = new List<byte>();
            res.Add((byte)_command);

            var arArray = new byte[4];
            for (var i=0; i < 4; i++)
            {
                arArray[i] = _arguments == null || i > _arguments.Length
                    ? (byte)0
                    : _arguments[i];
            }

            res.AddRange(arArray);

            return res.ToArray();
        }
    }
}
