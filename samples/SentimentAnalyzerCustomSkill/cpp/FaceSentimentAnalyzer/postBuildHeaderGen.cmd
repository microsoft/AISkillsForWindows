pushd %1
for %%x in (*.winmd) do winmdidl %%x /utf8 /metadata_dir:%2 /metadata_dir:. /outdir:.
for %%x in (*.idl) do midlrt %%x /metadata_dir %2 /ns_prefix always

popd