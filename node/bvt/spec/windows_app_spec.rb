require File.expand_path('../../rspec_helper', __FILE__)

describe 'Windows App' do

  it 'gets created ok' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output

    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"

    app = RHC::Commands::App.new $options, RHC::Config.new

    begin
      $options[:git] = false
      app.create(name, ['uhuru-dotnet-4.5'])
      output.string.should match /Your application '#{name}' is now available/
    ensure
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'is listed properly using rhc apps' do
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new
    $options[:git] = false

    begin
      app.create(name, ['uhuru-dotnet-4.5'])
      output = StringIO.new
      $terminal.instance_variable_set "@output", output
      apps = RHC::Commands::Apps.new $options, RHC::Config.new
      apps.run
      output.string.should match /#{name} @/
    ensure
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'is accessible via http' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new

    begin
      $options[:git] = false
      app.create(name, ['uhuru-dotnet-4.5'])
      url = /http:\/\/[^\s]+/.match(output.string)[0]

      sleep(10)

      open(url) do |html|
        html_content = html.read
        html_content.should match /Welcome to OpenShift with \.Net support/
      end
    ensure
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'can be cloned via Git' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new

    begin
      $options[:git] = false
      app.create(name, ['uhuru-dotnet-4.5'])

      $options[:app] = name

      puts `ssh-keyscan -H #{name}-#{$options[:domain_name]}.#{$cloud_host}`
      `ssh-keyscan -H #{name}-#{$options[:domain_name]}.#{$cloud_host} >> #{$known_hosts_file}`

      git_clone = RHC::Commands::GitClone.new $options, RHC::Config.new
      git_clone.run(name)

      Dir.exists?("./#{name}").should == true
    ensure
      FileUtils.rm_rf "./#{name}"
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'works with a Linux service cartridge' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new
    $options[:git] = false
    app.create(name, ['uhuru-dotnet-4.5'])

    begin
      output = StringIO.new
      $terminal.instance_variable_set "@output", output
      cartridge = RHC::Commands::Cartridge.new $options, RHC::Config.new

      $options[:app] = name
      cartridge.add('mysql-5.1')

      output.string.should match /MySQL 5\.1 database added\./
    ensure
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'can be updated via Git' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new
    $options[:repo] = File.join(Dir.tmpdir, name)

    begin
      $options[:git] = false
      app.create(name, ['uhuru-dotnet-4.5'])
      cartridge = RHC::Commands::Cartridge.new $options, RHC::Config.new
      $options[:app] = name
      cartridge.add('mysql-5.1')

      url = /http:\/\/[^\s]+/.match(output.string)[0]

      $options[:app] = name

      `ssh-keyscan -H #{name}-#{$options[:domain_name]}.#{$cloud_host} >> #{$known_hosts_file}`

      git_clone = RHC::Commands::GitClone.new $options, RHC::Config.new
      git_clone.run(name)

      `cp -R #{ File.expand_path('../../assets/aspnet_app/*', __FILE__) } #{$options[:repo]}`

      Dir.chdir($options[:repo]) do
        `git add .`
        `git commit -a -m 'test'`
        `git push`
      end

      sleep(10)

      open(url) do |html|
        html_content = html.read
        html_content.should match /It uses a MySql Database to store page visits/
      end
    ensure
      FileUtils.rm_rf $options[:repo]
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'accepts ssh to the Windows gear' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new

    begin
      $options[:git] = false
      app.create(name, ['uhuru-dotnet-4.5'])
      ssh_info = /SSH\ to:\s+[^\s]+/.match(output.string)[0].split.last
      ssh_host = ssh_info.split('@').last
      ssh_user = ssh_info.split('@').first

      Net::SSH.start( ssh_host, ssh_user) do |session|
        os_version = session.exec!("cmd /c ver")
        os_version.should match /Microsoft Windows/
      end
    ensure
      $options[:confirm] = true
      $options[:app] = name
      app.delete nil
    end
  end

  it 'gets deleted properly' do
    output = StringIO.new
    $terminal.instance_variable_set "@output", output
    name = "test#{(0...8).map { (65 + rand(26)).chr }.join.downcase }"
    app = RHC::Commands::App.new $options, RHC::Config.new

    $options[:git] = false
    app.create(name, ['uhuru-dotnet-4.5'])

    $options[:confirm] = true
    $options[:app] = name
    app.delete nil
  end
end