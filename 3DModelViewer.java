 
import java.applet.Applet;
import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.event.*;
import java.awt.GraphicsConfiguration;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileReader;
import java.io.FileWriter;

import com.sun.j3d.loaders.Scene;
import com.sun.j3d.loaders.objectfile.ObjectFile;
import com.sun.j3d.utils.applet.MainFrame;
import com.sun.j3d.utils.universe.*;
import javax.media.j3d.*;
import javax.swing.AbstractButton;
import javax.swing.BoxLayout;
import javax.swing.JButton;
import javax.swing.JFileChooser;
import javax.swing.JPanel;
import javax.swing.JSlider;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;
import javax.swing.filechooser.FileFilter;
import javax.swing.filechooser.FileNameExtensionFilter;
import javax.vecmath.*;
//------------------------------------------------------------------------v
import com.sun.j3d.utils.behaviors.mouse.MouseRotate;
//------------------------------------------------------------------------^

public class Assign1 extends Applet implements ChangeListener {
	//Declare our variables that we'll need globally
    private SimpleUniverse u = null;
    float x = 0;
    float y = 0;
    float z = 0;
    
    TransformGroup trans = new TransformGroup();
    BranchGroup objRoot = new BranchGroup();
      
    
    public BranchGroup createSceneGraph(File openfile) {
    	// This opens the file, and returns the BranchGroup of the file contents
    	
    	trans = new TransformGroup();
        // Create the root of the branch graph
    	objRoot.detach(); // detach the old model
    	objRoot = new BranchGroup();//make a new branchgroup
        // Create a Transform group to scale all objects so they
        // appear in the scene.
        TransformGroup objScale = new TransformGroup();
        Transform3D t3d = new Transform3D();
        t3d.setScale(0.7);
        objScale.setTransform(t3d);
        objRoot.addChild(objScale); //Make the model and appropriate size
 
        //Mouse rotate 
        MouseRotate    mr = new MouseRotate();       
        // tell the behavior which transform Group it is operating on
        mr.setTransformGroup(trans);
        
        // create the bounds for rotate behavior (centered at the origin) 
        BoundingSphere bounds = new BoundingSphere(new Point3d(0.0,0.0,0.0), 100.0);
        mr.setSchedulingBounds(bounds);
        
        // add the Rotate Behavior to the root.(not the transformGroup)
        objRoot.addChild(mr);
        
        // since the transform in the transformGroup will be changing when we rotate the object
        // we need to explicitly allow the transform to be read, and written.
        trans.setCapability(trans.ALLOW_TRANSFORM_READ);
        trans.setCapability(trans.ALLOW_TRANSFORM_WRITE);
//------------------------------------------------------------------------^           
        if(openfile!=null){
        	//if this isn't the first time around, then try to open a file
        	try{
        		//Create a scene, set the flags
        		Scene s = null;
        		ObjectFile f = new ObjectFile();
        		f.setFlags(ObjectFile.RESIZE | ObjectFile.TRIANGULATE | ObjectFile.STRIPIFY );
        		
        		//add a \n to the end of the file, because the OBJ reader needs a nice newline.
        		BufferedReader reader = new BufferedReader(new FileReader(openfile));
        		StringBuilder contents = new StringBuilder();

        		String line = null;
        		while ((line=reader.readLine()) != null) {
        		  contents.append(line);
        		  contents.append("\n");
        		}
        		
        		//write it out... it's easier to do this than parse the file to get the 'extra' file (lile
        		FileWriter fstream = new FileWriter(openfile.getAbsolutePath());
        		BufferedWriter out = new BufferedWriter(fstream);
        		out.write(contents.toString());
        		//Close the output stream
        		out.close();
                
        		//load our freshly new-lined file
        		s=f.load(openfile.getAbsolutePath());
        		trans.addChild(s.getSceneGroup());
        		reader.close();
        	}
        	catch (Exception e){
        		//If we fail, print why.
        		e.printStackTrace();
        	}
        }
        
        // add the transform group (and therefore also the cube) to the scene
        objRoot.addChild(trans);
        
        //Set the background Color
        Color3f bgColor = new Color3f(0.05f, 0.05f, 0.5f);
        Background bgNode = new Background(bgColor);
        bgNode.setApplicationBounds(bounds);
        objRoot.addChild(bgNode);
        
        //Set the ambient color
      	Color3f ambientColor = new Color3f (0.35f, 0.35f, 0.35f);
      	AmbientLight ambientLightNode = new AmbientLight (ambientColor);
      	ambientLightNode.setInfluencingBounds (bounds);
      	objRoot.addChild (ambientLightNode);
      	
        // Set up the directional lights (like a spotlight)
        Color3f light1Color = new Color3f(0.1f, 0.1f, 0.1f);
        Vector3f light1Direction = new Vector3f(10.0f, 10.0f, 10.0f);
        Color3f light2Color = new Color3f(0.1f, 0.1f, 0.1f);
        Vector3f light2Direction = new Vector3f(4.0f, -7.0f, -12.0f);

        DirectionalLight light1 = new DirectionalLight(light1Color,
            light1Direction);
        light1.setInfluencingBounds(bounds);
        objRoot.addChild(light1);

        DirectionalLight light2 = new DirectionalLight(light2Color,
            light2Direction);
        light2.setInfluencingBounds(bounds);
        objRoot.addChild(light2);
        
        //Allow us to remove the object if we need to
    	objRoot.setCapability(BranchGroup.ALLOW_DETACH);
        // Have Java 3D perform optimizations on this scene graph.
        objRoot.compile();
        return objRoot;
    }

    public Assign1() {
    }

    public void init() {
    	//When the object is first created, lets do some bookeeping
    	File openfile = null; //set the filename to null so we don't try to open anything until a button is hit
    	JPanel buttons = new JPanel(); //our panel for buttons (open and the wireframe)
    	buttons.setLayout(new BoxLayout(buttons, BoxLayout.PAGE_AXIS));//Set the button layout
    
    	//Set layout for the main panel to be Border
        setLayout(new BorderLayout());
        
        //Load and set the default graphics options
        GraphicsConfiguration config =
           SimpleUniverse.getPreferredConfiguration();
        Canvas3D c = new Canvas3D(config);
        add(c, BorderLayout.CENTER);
        
        //Add our sliders (top is zoom, bottom is x, left is y
        JSlider zoom = new JSlider(JSlider.HORIZONTAL, -30, 10, -10);
        zoom.setMajorTickSpacing(1);
        zoom.setName("z");
        zoom.setPaintTicks(true);
        zoom.addChangeListener(this);
        add (zoom, BorderLayout.NORTH);
        
        JSlider xslide = new JSlider(JSlider.HORIZONTAL, -30, 30, 0);
        xslide.setMajorTickSpacing(1);
        xslide.setPaintTicks(true);
        xslide.setName("x");
        xslide.addChangeListener(this);
        add (xslide, BorderLayout.PAGE_END);
        
        JSlider yslide = new JSlider(JSlider.VERTICAL, -30, 30, 0);
        yslide.setMajorTickSpacing(1);
        yslide.setPaintTicks(true);
        yslide.setName("y");
        yslide.addChangeListener(this);
        add (yslide, BorderLayout.WEST);
        
        //Open button
        JButton opener = new JButton("Open");
        opener.setMaximumSize(new Dimension(100,50));
        opener.setVerticalTextPosition(AbstractButton.BOTTOM);
        opener.setHorizontalTextPosition(AbstractButton.CENTER);
        opener.addActionListener(new ActionListener() {
        	
            public void actionPerformed(ActionEvent e)
            {
                //Execute when button is pressed
                JFileChooser fileopen = new JFileChooser();
                FileFilter filter = new FileNameExtensionFilter("obj files", "obj");
                fileopen.addChoosableFileFilter(filter); // Open the file picker

                int ret = fileopen.showDialog(null, "Open file"); // Save if successful

                if (ret == JFileChooser.APPROVE_OPTION) { //If successful
                  File openfile = fileopen.getSelectedFile();// retrieve file
                  BranchGroup scene = createSceneGraph(openfile); //open the file
                  u.addBranchGraph(scene); // add the scene to the universe
                }
                
            }
        });
        buttons.add (opener); // add the open button to the layout
        
        //The wireframe / point toggle button
        JButton toggle = new JButton("Wire / Point");
        toggle.setMaximumSize(new Dimension(100,50));
        toggle.setVerticalTextPosition(AbstractButton.BOTTOM);
        toggle.setHorizontalTextPosition(AbstractButton.CENTER);
        buttons.add(toggle);

    	add(buttons, BorderLayout.EAST);
        // Create a simple scene and attach it to the virtual universe
        BranchGroup scene = createSceneGraph(openfile);

        u = new SimpleUniverse(c);

        // This will move the ViewPlatform back a bit so the
        // objects in the scene can be viewed.
        u.getViewingPlatform().setNominalViewingTransform();

        // add the objects to the universe
        u.addBranchGraph(scene);
    }

    public void destroy() {
        u.removeAllLocales();
    }

    //
    // The following allows Assign1 to be run as an application
    // as well as an applet
    //
    public static void main(String[] args) {
        new MainFrame(new Assign1(), 640, 480); //640x480 is likely the smallest screensize, so a safe default
    }

	public void stateChanged(ChangeEvent e) {
		//If the sliders are moved, then transform the object in the x, y, x dimensions.
		JSlider source =(JSlider) e.getSource(); //figure out which slider
		if(source.getName()=="z"){
			z = (float) 0.3F * source.getValue(); // perform some scaling so the scrolling feels natural
		}
		if(source.getName()=="x"){
			x = (float) 0.1F * source.getValue();
		}
		if(source.getName()=="y"){
			y = (float) 0.1F * source.getValue();
		}
		Transform3D transform = new Transform3D();
		transform.setTranslation(new Vector3f(x,y,z));
		trans.setTransform(transform); //commit the transform
	}
}
