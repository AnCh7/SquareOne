using System;

namespace Sq1.Widgets.Exceptions {
	public partial class ExceptionsControl {
		void exceptionsTreeListViewCustomize() {
			//v2	http://stackoverflow.com/questions/9802724/how-to-create-a-multicolumn-treeview-like-this-in-c-sharp-winforms-app/9802753#9802753
			this.treeExceptions.CanExpandGetter = delegate(object o) {
				var ex = o as Exception;
				if (ex == null) return false;
				return ex.InnerException != null;
			};
			this.treeExceptions.ChildrenGetter = delegate(object o) {
				var ex = o as Exception;
				if (ex == null) return null;
				return new Exception[] { ex.InnerException };
			};
			this.olvTime.AspectGetter = delegate(object o) {
				var ex = o as Exception;
				if (ex == null) return o.ToString();
				string messageOrNameOrTime = ex.Message;
				if (this.DataSnapshot.TreeShowsTimesInsteadOfMessages) {
					if (this.ExceptionTimes.ContainsKey(ex)) {
						DateTime exTime = this.ExceptionTimes[ex];
						messageOrNameOrTime = exTime.ToString("HH:mm:ss.fff ddd dd MMM yyyy");
					} else {
						messageOrNameOrTime = ex.GetType().Name;
					}
				}
				return messageOrNameOrTime;
			};
			this.olvTime.ImageGetter = delegate(object o) {
				return null;
			};
		}
	}
}