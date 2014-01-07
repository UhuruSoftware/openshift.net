require File.expand_path('../../rspec_helper', __FILE__)

describe 'Windows Cartridge' do
  it 'should be listed in the list of OpenShift cartridges' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output

    cartridge = RHC::Commands::Cartridge.new $options, RHC::Config.new
    cartridge.list

    output.string.should match /uhuru-dotnet-4\.5/
  end
end