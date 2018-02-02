dim doc
set doc = CreateObject("XYNetComClient.1")
doc.SetReadTimeout 10
if doc.Connect("Lobster", 3000) = 0 then
	wscript.echo doc.GetLastError
	wscript.echo doc.GetLastErrorCode
	wscript.quit
end if
if doc.ReceiveData(10000000, false)  then
	wscript.echo doc.GetStringData
else
	wscript.echo doc.GetLastError
end if
doc.SendStringData "Hello, world.  This is a test.  It is only a test.  If it were not, you will hear some screaming coming from all directions."
if doc.ReceiveData(10000000, false)  then
	wscript.echo doc.GetStringData
else
	wscript.echo doc.GetLastError
end if
'doc.Reset
set doc = nothing