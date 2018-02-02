
XYNetComClientps.dll: dlldata.obj XYNetComClient_p.obj XYNetComClient_i.obj
	link /dll /out:XYNetComClientps.dll /def:XYNetComClientps.def /entry:DllMain dlldata.obj XYNetComClient_p.obj XYNetComClient_i.obj \
		kernel32.lib rpcndr.lib rpcns4.lib rpcrt4.lib oleaut32.lib uuid.lib \

.c.obj:
	cl /c /Ox /DWIN32 /D_WIN32_WINNT=0x0400 /DREGISTER_PROXY_DLL \
		$<

clean:
	@del XYNetComClientps.dll
	@del XYNetComClientps.lib
	@del XYNetComClientps.exp
	@del dlldata.obj
	@del XYNetComClient_p.obj
	@del XYNetComClient_i.obj
