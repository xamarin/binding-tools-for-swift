2019-07-09
Error in wrapping for the type "CGPoints?"
CGPoints is (I think) a typealias for (CGPoint, CGPoint). We try to use the
desugared type name and the swift compiler complains.
https://github.com/xamarin/maccore/issues/1862
