import Foundation

public func dateNew (retval: UnsafeMutablePointer<Date>)
{
	retval.initialize(to: Date())
}	

public func dateNewFromNSDate (retval: UnsafeMutablePointer<Date>, from: NSDate)
{
	retval.initialize(to: from as Date)
}

public func dateNewFromInterval(retval: UnsafeMutablePointer<Date>, timeIntervalSinceNow: Double)
{
    retval.initialize(to: Date(timeIntervalSinceNow: timeIntervalSinceNow));
}

public func dateNewFromIntervalSince(retval: UnsafeMutablePointer<Date>, timeInterval: Double, since: UnsafePointer<Date>)
{
    retval.initialize(to: Date(timeInterval: timeInterval, since: since.pointee));
}

public func dateNewSinceReference(retval: UnsafeMutablePointer<Date>, timeIntervalSinceReferenceDate: Double)
{
    retval.initialize(to: Date(timeIntervalSinceReferenceDate: timeIntervalSinceReferenceDate));
}

public func dateNewSince1970(retval: UnsafeMutablePointer<Date>, timeIntervalSince1970: Double)
{
    retval.initialize(to: Date(timeIntervalSince1970: timeIntervalSince1970));
}

public func dateToNSDate (this: UnsafeMutablePointer<Date>) -> NSDate
{
	return this.pointee as NSDate
}

public func dateFromNSDate (retval: UnsafeMutablePointer<Date>, from: NSDate)
{
	retval.initialize(to: from as Date)
}