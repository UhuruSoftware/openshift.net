
class WebInterface < Sinatra::Base
  set :root, File.expand_path("..", __FILE__)
  set :views, File.expand_path("../views", __FILE__)
  set :public_folder, File.expand_path("../public", __FILE__)

  set :bind => 'localhost'
  set :port => '17171'

  error do
  end

  get '/' do
<<SCRIPT
<h1>mcollective windows openshift agent</h1>

#{$dev_debug_msg.reverse.join('<br /><hr />')}

<script type='text/javascript' >

    setTimeout(function() {window.location.reload(true);}, 3000);

</script>
SCRIPT

  end
end