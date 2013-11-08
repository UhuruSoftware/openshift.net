require 'open3'
require 'json'

module MCollective
  module Agent
    class Powershell

      def self.run_command(command ,args)
        script = File.join(File.dirname(__FILE__), "cmdlets", "#{command.to_s.gsub('_', '-')}.ps1")
        cmd = "powershell.exe -ExecutionPolicy Bypass -InputFormat None -noninteractive -file #{script} \"#{args}\" 2>&1"
        output = ""
        exitcode = 0
        Open3.popen3(cmd) do |stdin, stdout, stderr, wait_thr|
          output = stdout.read
          exitcode = wait_thr.value.exitstatus
        end
        return exitcode, output
      end
    end
  end
end
