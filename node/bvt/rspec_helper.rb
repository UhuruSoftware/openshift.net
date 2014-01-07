require 'rspec'
require 'rhc/commands'
require 'fileutils'
require 'open-uri'
require 'net/ssh'

RHC::Commands.load

$options = Commander::Command::Options.new

$known_hosts_file = ENV['UHURU_KNOWN_HOSTS']

$cloud_host = ENV['UHURU_CLOUD_HOST']

$options[:domain_name] = ENV['UHURU_DOMAIN_NAME']
$options[:server] = ENV['UHURU_SERVER']
$options[:rhlogin] = ENV['UHURU_RHLOGIN']
$options[:password] = ENV['UHURU_PASSWORD']

$options[:insecure] = true

