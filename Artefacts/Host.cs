using System;
using System.Diagnostics;
using System.IO;

namespace Artefacts
{
	/// <summary>
	/// Host.
	/// </summary>
	public class Host : IEquatable<Host>
	{
		#region Static members
		/// <summary>
		/// Gets the current <see cref="Host"/> 
		/// </summary>
		public static Host Current {
			get
			{
				if (_current == null)
					_current = new Host(GetHostId());
				return _current;
			}
		}
		private static Host _current = null;

		/// <summary>
		/// Gets the host identifier.
		/// </summary>
		/// <returns>The host identifier.</returns>
		public static string GetHostId()
		{
			string hostId;
			Process procGetHost = Process.Start(
				new ProcessStartInfo("hostid")
				{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			});
			procGetHost.WaitForExit(1111);
			hostId = procGetHost.StandardOutput.ReadLine();
			if (string.IsNullOrEmpty(hostId))
				throw new InvalidDataException("Unexpected output data from hostid command");
			return hostId;
		}
		#endregion

		public virtual string HostId { get; private set; }

		public string Machine { get; private set; }

		public string Domain { get; private set; }

		public virtual int ConnectionId { get; set; }

		public virtual bool Connected { get { return ConnectionId != default(int); } }

		public virtual DateTime ConnectTime { get; set; }

		public virtual TimeSpan ConnectionAge { get { return ConnectTime == DateTime.MinValue ? TimeSpan.Zero : DateTime.Now - ConnectTime; } }

		public Host(string host)
		{
			if (string.IsNullOrWhiteSpace(host))
				throw new ArgumentOutOfRangeException("host");
			ConnectionId = -1;
			ConnectTime = DateTime.MinValue;
			HostId = host;
			string[] hostComponents = host.Split(new char[] { '.' }, 2);
			Machine = hostComponents[0];
			if (hostComponents.Length > 1)
				Domain = hostComponents[0];
		}

		#region IEquatable implementation

		public bool Equals(Host other)
		{
			return string.Equals(HostId, other.HostId);
		}

		public override bool Equals(object other)
		{
			if (other == null || Object.ReferenceEquals(this, other))
				return true;
			if (!typeof(Host).IsAssignableFrom(other.GetType()))
				return false;
			return Equals((Host)other);
		}
		#endregion
		
		public override int GetHashCode()
		{
			return HostId.GetHashCode();
		}
		
		public override string ToString()
		{
			return HostId;// string.Format("[Host: HostId={0}, Machine={1}, Domain={2}, ConnectionId={3}, Connected={4}, ConnectTime={5}, ConnectionAge={6}]", HostId, Machine, Domain, ConnectionId, Connected, ConnectTime, ConnectionAge);
		}
		public static implicit operator Host(string host)
		{
			return new Host(host);
		}
	}
}